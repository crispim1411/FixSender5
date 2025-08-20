using NLog;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace FixSender5.FixEngine;

public class Acceptor : IApplication
{
    private bool _isLoggedOn = false;
    public bool IsLoggedOn => _isLoggedOn;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private const string _configPath = "Config/acceptor.cfg";

    public void ToAdmin(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[ToAdmin] Acceptor - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromAdmin(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[FromAdmin] Acceptor - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void ToApp(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[ToApp] Acceptor - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void FromApp(Message message, SessionID sessionId)
    {
        Console.WriteLine($"[FromApp] Acceptor - Session: {sessionId}, Message: {FormatMessage(message)}");
    }

    public void OnCreate(SessionID sessionId)
    {
        Console.WriteLine($"[OnCreate] Acceptor - Session created: {sessionId}");
    }

    public void OnLogout(SessionID sessionId)
    {
        _isLoggedOn = false;
        Console.WriteLine($"[OnLogout] Acceptor - Session logged out: {sessionId}");
    }

    public void OnLogon(SessionID sessionId)
    {
        _isLoggedOn = true;
        Console.WriteLine($"[OnLogon] Acceptor - Session logged on: {sessionId}");
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
        var acceptor = new ThreadedSocketAcceptor(this, storeFactory, settings, logFactory);

        _logger.Info("Iniciando o FIX acceptor...");
        acceptor.Start();
        
        while (!cancellationToken.IsCancellationRequested)
            Task.Delay(1000, cancellationToken).Wait(cancellationToken);
        _logger.Info("FIX acceptor cancelado.");
        acceptor.Stop();
        acceptor.Dispose();
    }
}