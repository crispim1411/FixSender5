using NLog;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace FixSender5.FixEngine;

public class Initiator : IApplication
{
    private bool _isLoggedOn;
    public bool IsLoggedOn => _isLoggedOn;

    public event Action OnSessionLogon;
    public event Action OnSessionLogoff;
    
    private const string _configPath = "Config/initiator.cfg";
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    
    public void ToAdmin(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[ToAdmin] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromAdmin(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[FromAdmin] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void ToApp(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[ToApp] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromApp(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[FromApp] Initiator - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void OnCreate(SessionID sessionId)
    {
        Console.WriteLine($"[OnCreate] Initiator - Session created: {sessionId}");
    }

    public void OnLogout(SessionID sessionId)
    {
        _isLoggedOn = false;
        OnSessionLogoff?.Invoke();
        Console.WriteLine($"[OnLogout] Initiator - Session logged out: {sessionId}");
    }

    public void OnLogon(SessionID sessionId)
    {
        _isLoggedOn = true;
        OnSessionLogon?.Invoke();
        Console.WriteLine($"[OnLogon] Initiator - Session logged on: {sessionId}");
    }
    
    private static string FormatMessage(Message message)
    {
        // Converte a mensagem FIX para um formato customizado com o separador "|"
        var messageString = message.ToString();
        return messageString.Replace("\x01", "|");
    }
    
    public void Start(CancellationToken cancellationToken)
    {
        var settings = new SessionSettings(_configPath);
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);
        var initiator = new SocketInitiator(this, storeFactory, settings, logFactory);

        _logger.Info("Iniciando o Initiator...");
        initiator.Start();
        
        while (!cancellationToken.IsCancellationRequested)
            Task.Delay(1000, cancellationToken).Wait(cancellationToken);    
        _logger.Info("FIX initiator cancelado.");
        initiator.Stop();
        initiator.Dispose();
    }
}