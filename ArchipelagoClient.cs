using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;

namespace DemonicMahjongArchipelago;

public class ArchipelagoClient
{
    public const string APVersion = "0.5.0";
    private const string Game = "Demonic Mahjong";

    public static bool Authenticated;
    private bool attemptingConnection;

    public static ArchipelagoData ServerData = new();
    private DeathLinkHandler DeathLinkHandler;
    private ArchipelagoSession session;

    public void checkLocation(int id)
    {
        session.Locations.CompleteLocationChecks(id);
    }

    /// <summary>
    /// call to connect to an Archipelago session. Connection info should already be set up on ServerData
    /// </summary>
    /// <returns></returns>
    public void Connect()
    {
        if (Authenticated || attemptingConnection) return;

        try
        {
            session = ArchipelagoSessionFactory.CreateSession(ServerData.Uri);
            SetupSession();
        }
        catch (Exception e)
        {
           ArchipelagoPlugin.BepinLogger.LogError(e);
        }
        ArchipelagoPlugin.BepinLogger.LogMessage("Session created, attempting connection");
        TryConnect();
    }

    /// <summary>
    /// add handlers for Archipelago events
    /// </summary>
    private void SetupSession()
    {
        //session.MessageLog.OnMessageReceived += message => ArchipelagoConsole.LogMessage(message.ToString());
        session.Items.ItemReceived += OnItemReceived;
        session.Socket.ErrorReceived += OnSessionErrorReceived;
        session.Socket.SocketClosed += OnSessionSocketClosed;
    }

    /// <summary>
    /// attempt to connect to the server with our connection info
    /// </summary>
    private void TryConnect()
    {
        try
        {
            // it's safe to thread this function call but unity notoriously hates threading so do not use excessively
            //ThreadPool.QueueUserWorkItem(
            //    _ => HandleConnectResult(
            //        session.TryConnectAndLogin(
            //            Game,
            //            ServerData.SlotName,
            //            ItemsHandlingFlags.AllItems, 
            //            new Version(APVersion),
            //            password: ServerData.Password,
            //            requestSlotData: true // ServerData.NeedSlotData
            //        )));
            HandleConnectResult(
                    session.TryConnectAndLogin(
                        Game,
                        ServerData.SlotName,
                        ItemsHandlingFlags.AllItems, 
                        new Version(APVersion),
                        password: ServerData.Password,
                        requestSlotData: true // ServerData.NeedSlotData
                    ));
        }
        catch (Exception e)
        {

            ArchipelagoPlugin.BepinLogger.LogMessage("Connection failed");
            ArchipelagoPlugin.BepinLogger.LogError(e);
            HandleConnectResult(new LoginFailure(e.ToString()));
            attemptingConnection = false;
        }
    }

    /// <summary>
    /// handle the connection result and do things
    /// </summary>
    /// <param name="result"></param>
    private void HandleConnectResult(LoginResult result)
    {
        string outText;
        if (result.Successful)
        {
            var success = (LoginSuccessful)result;

            ServerData.SetupSession(success.SlotData, session.RoomState.Seed);
            Authenticated = true;

            DeathLinkHandler = new(session.CreateDeathLinkService(), ServerData.SlotName);
            //session.Locations.CompleteLocationChecksAsync(ServerData.CheckedLocations.ToArray());
            outText = $"Successfully connected to {ServerData.Uri} as {ServerData.SlotName}!";

            ArchipelagoPlugin.BepinLogger.LogMessage(outText);
            //ArchipelagoConsole.LogMessage(outText);

            GameData.OnConnectSetup(success);
        }
        else
        {
            var failure = (LoginFailure)result;
            outText = $"Failed to connect to {ServerData.Uri} as {ServerData.SlotName}.";
            ArchipelagoPlugin.BepinLogger.LogError(outText);
            outText = failure.Errors.Aggregate(outText, (current, error) => current + $"\n    {error}");

            ArchipelagoPlugin.BepinLogger.LogError(outText);

            Authenticated = false;
            Disconnect();
        }

        //ArchipelagoConsole.LogMessage(outText);
        attemptingConnection = false;
    }

    /// <summary>
    /// something went wrong, or we need to properly disconnect from the server. cleanup and re null our session
    /// </summary>
    internal async Task Disconnect()
    {
       ArchipelagoPlugin.BepinLogger.LogDebug("disconnecting from server...");
        session?.Socket.DisconnectAsync();
        session = null;
        Authenticated = false;
        await GameData.SaveAsync();
    }

    public void SendMessage(string message)
    {
        session.Socket.SendPacketAsync(new SayPacket { Text = message });
    }

    /// <summary>
    /// we received an item so reward it here
    /// </summary>
    /// <param name="helper">item helper which we can grab our item from</param>
    private void OnItemReceived(ReceivedItemsHelper helper)
    {
        if (!GameData.ConnectSetupComplete) return;
        var receivedItem = helper.DequeueItem();

        if (helper.Index <= ServerData.Index) return;

        ServerData.Index++;
        GameData.LastProcessedItem = ServerData.Index;

        // TODO reward the item here
        // if items can be received while in an invalid state for actually handling them, they can be placed in a local
        // queue/collection to be handled later
        GameData.receiveItem(receivedItem);

    }

    public void CheckItems()
    {
        while (session.Items.Any())
        {
            OnItemReceived((ReceivedItemsHelper)session.Items);
        }
    }

    /// <summary>
    /// something went wrong with our socket connection
    /// </summary>
    /// <param name="e">thrown exception from our socket</param>
    /// <param name="message">message received from the server</param>
    private void OnSessionErrorReceived(Exception e, string message)
    {
       ArchipelagoPlugin.BepinLogger.LogError(e);
        //ArchipelagoConsole.LogMessage(message);
    }

    /// <summary>
    /// something went wrong closing our connection. disconnect and clean up
    /// </summary>
    /// <param name="reason"></param>
    private void OnSessionSocketClosed(string reason)
    {
        ArchipelagoPlugin.BepinLogger.LogError($"Connection to Archipelago lost: {reason}");
        Disconnect();

        ArchipelagoUI.CreateInfoPanel("Disconnected", "Client has been disconnected from archipelago server.", true);
    }
}