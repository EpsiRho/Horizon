using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Horizon
{
    public class Connection
    {
        public string filename { get; set; }
        public string name { get; set; }
        public ManualResetEvent handler;
        public Socket sock;
        public bool progressBar;
        public Connection()
        {
            this.filename = "null";
        }
    }
    public class ConnectionsViewModel
    {
        private Connection defaultContact = new Connection();
        public Connection DefaultContact { get { return this.defaultContact; } }

        private ObservableCollection<Connection> connections = new ObservableCollection<Connection>();
        public ObservableCollection<Connection> Connections { get { return this.connections; } }
        public ConnectionsViewModel()
        {
        }
        public Connection AddConnection(string Filename, string Name, Socket Sock)
        {
            this.connections.Add(new Connection()
            {
                filename = Filename,
                name = Name,
                handler = new ManualResetEvent(false),
                sock = Sock,
                progressBar = false
            });
            return connections[connections.Count() - 1];
        }
        public void RemoveConnection(Connection item)
        {
            connections.Remove(item);
        }
        public int GetIndex(Connection item)
        {
            return connections.IndexOf(item);
        }
    }
}
