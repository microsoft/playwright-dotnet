using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport
{
    /// <summary>
    /// Base Channel owner class.
    /// </summary>
    public class ChannelOwnerBase : IChannelOwner
    {
        private readonly Connection _connection;
        private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new ConcurrentDictionary<string, IChannelOwner>();

        internal ChannelOwnerBase(IChannelOwner parent, string guid) : this(parent, null, guid)
        {
        }

        internal ChannelOwnerBase(IChannelOwner parent, Connection connection, string guid)
        {
            _connection = parent?.Connection ?? connection;

            Guid = guid;
            Parent = parent;

            _connection.Objects[guid] = this;
            if (Parent != null)
            {
                Parent.Objects[guid] = this;
            }
        }

        /// <inheritdoc/>
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => null;

        /// <inheritdoc/>
        ConcurrentDictionary<string, IChannelOwner> IChannelOwner.Objects => _objects;

        internal string Guid { get; set; }

        internal IChannelOwner Parent { get; set; }

        /// <inheritdoc/>
        void IChannelOwner.DisposeOwner()
        {
            Parent?.Objects?.TryRemove(Guid, out var _);
            _connection?.Objects.TryRemove(Guid, out var _);

            foreach (var item in _objects.Values)
            {
                item.DisposeOwner();
                _objects.Clear();
            }
        }
    }
}
