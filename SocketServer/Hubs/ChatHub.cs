using Microsoft.AspNetCore.SignalR;
using SocketServer.Models;

namespace SocketServer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IDictionary<string, UserConnection> _connections;

        public ChatHub(IDictionary<string, UserConnection> connections)
        {
            _connections = connections;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                _connections.Remove(Context.ConnectionId);

                await SendGroups();
                await SendConnectedUsers(userConnection.Room);
            }

            await base.OnDisconnectedAsync(exception);
        }


        public async Task JoinRoom(UserConnection userConnection)
        {
            await Groups
                .AddToGroupAsync(Context.ConnectionId, userConnection.Room);

            userConnection.Id = Context.ConnectionId;

            _connections[Context.ConnectionId] = userConnection;

            await SendGroups();
            await SendConnectedUsers(userConnection.Room);
        }

        public async Task SendMessage(string message)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                await Clients
                    .Group(userConnection.Room)
                    .SendAsync("ReceivedMessage", userConnection, message);
            }
        }

        public Task SendGroups()
        {
            var users = _connections
              .Values
              .Select(x => x.Room)
              .Distinct();

            return Clients.All.SendAsync("RoomsAvailable", users);
        }

        public Task SendConnectedUsers(string room)
        {
            var users = _connections
                .Values
                .Where(x => x.Room == room)
                .Select(x => x.UserName);

            return Clients.Group(room).SendAsync("UsersInRoom", users);
        }
    }
}
