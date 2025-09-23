using System.Windows.Threading;
using NLog;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace FixSender5.FixEngine;

public class Initiator : IApplication
{
    private const string _configPath = "Config/initiator.cfg";
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly DispatcherTimer _seqNumTimer;
    private SessionID? _currentSessionID;
    
    public string SenderCompId { get; init; }
    public string TargetCompId { get; init; }
    public int Port { get; init; }
    public string Host { get; init; }
    
    public event Action? OnSessionLogon;
    public event Action? OnSessionLogout;
    public event Action<Message>? OnInboundMessage;
    public event Action<Message>? OnOutboundMessage;
    public event Action<(ulong, ulong)>? OnChangeSeqNum;

    public Initiator()
    {
        _seqNumTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1) 
        };
        _seqNumTimer.Tick += UpdateSequenceNumbers;
    }

    public void ToAdmin(Message message, SessionID sessionId)
    {
        OnOutboundMessage?.Invoke(message);
        _logger.Info($"[ToAdmin] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromAdmin(Message message, SessionID sessionId)
    {
        OnInboundMessage?.Invoke(message);
        _logger.Info($"[FromAdmin] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void ToApp(Message message, SessionID sessionId)
    {
        OnOutboundMessage?.Invoke(message);
        var inSeqNum = Session.LookupSession(sessionId)?.NextSenderMsgSeqNum ?? 0;
        var outSeqNum = Session.LookupSession(sessionId)?.NextTargetMsgSeqNum ?? 0;
        OnChangeSeqNum?.Invoke((inSeqNum, outSeqNum));
        _logger.Info($"[ToApp] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromApp(Message message, SessionID sessionId)
    {
        OnInboundMessage?.Invoke(message);
        var inSeqNum = Session.LookupSession(sessionId)?.NextSenderMsgSeqNum ?? 0;
        var outSeqNum = Session.LookupSession(sessionId)?.NextTargetMsgSeqNum ?? 0;
        OnChangeSeqNum?.Invoke((inSeqNum, outSeqNum));
        _logger.Info($"[FromApp] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void OnCreate(SessionID sessionId)
    {
        _logger.Info($"[OnCreate] Initiator - Session created: {sessionId}");
    }

    public void OnLogout(SessionID sessionId)
    {
        _currentSessionID = null;
        OnSessionLogout?.Invoke();
        _seqNumTimer?.Stop();
        _logger.Info($"[OnLogout] Initiator - Session logged out: {sessionId}");
    }

    public void OnLogon(SessionID sessionId)
    {
        _currentSessionID = sessionId;
        OnSessionLogon?.Invoke();
        _seqNumTimer?.Start();
        _logger.Info($"[OnLogon] Initiator - Session logged on: {sessionId}");
    }
    
    private static string FormatMessage(Message message)
    {
        // Converte a mensagem FIX para um formato customizado com o separador "|"
        var messageString = message.ToString();
        return messageString.Replace("\x01", "|");
    }
    
    private void UpdateSequenceNumbers(object? sender, EventArgs e)
    {
        if (_currentSessionID == null) return;
        
        var session = Session.LookupSession(_currentSessionID);
        if (session == null) return;
        
        var inSeqNum = session.NextSenderMsgSeqNum;
        var outSeqNum = session.NextTargetMsgSeqNum;
        OnChangeSeqNum?.Invoke((inSeqNum, outSeqNum));
    }
    
    public void SendMessage(Message message)
    {
        while (true)
        {
            if (_currentSessionID != null && Session.SendToTarget(message, _currentSessionID))
                break;
            Task.Delay(1000).Wait(1000);
        }
    }
    
    public async Task Start(CancellationToken cancellationToken)
    {
        var settings = new SessionSettings();
        SetValues(settings);
        
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);
        var initiator = new SocketInitiator(this, storeFactory, settings, logFactory);

        try
        {
            _logger.Info("Iniciando o Initiator...");
            initiator.Start();

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.Info("FIX initiator cancelado.");
        }
        finally
        {
            initiator.Stop();
            initiator.Dispose();
            _seqNumTimer?.Stop();
        }
    }
    
    private void SetValues(SessionSettings settings)
    {
        var defaultDict = new SettingsDictionary();
        defaultDict.SetString("ConnectionType","initiator");
        defaultDict.SetString("ReconnectInterval", "60");
        defaultDict.SetString("FileStorePath", @"store");
        defaultDict.SetString("FileLogPath", "log");
        defaultDict.SetString("StartTime", "00:00:00");
        defaultDict.SetString("EndTime", "00:00:00");
        defaultDict.SetBool("UseDataDictionary", true);
        defaultDict.SetString("AppDataDictionary", @"Config/FIX50.xml");
        defaultDict.SetString("TransportDataDictionary", @"Config/FIXT11.xml");
        defaultDict.SetString("DefaultApplVerID", "FIX.5.0");
        defaultDict.SetString("SocketConnectHost", Host);
        defaultDict.SetString("SocketConnectPort", Port.ToString());
        defaultDict.SetString("LogoutTimeout", "5");
        defaultDict.SetBool("ResetOnLogon", true);
        defaultDict.SetBool("ResetOnDisconnect", true);

        settings.Set(defaultDict);
        
        var sessionDict = new SettingsDictionary();
       
        // tentar remover e deixar so id
        sessionDict.SetString("BeginString", "FIXT.1.1");
        sessionDict.SetString("SenderCompID", SenderCompId);    
        sessionDict.SetString("TargetCompID", TargetCompId);
        sessionDict.SetString("HeartBtInt", "60");


        var sId = new SessionID("FIXT.1.1", SenderCompId, TargetCompId);
        settings.Set(sId, sessionDict);

    }
}