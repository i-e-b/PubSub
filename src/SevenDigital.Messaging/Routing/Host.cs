using System;

namespace SevenDigital.Messaging.Routing
{
	public class Host: IMessagingHost, IEquatable<Host>
	{
		readonly string _machineName;

		public Host(string machineName)
		{
			_machineName = machineName;
		}

		public override string ToString()
		{
			return _machineName;
		}

		#region Equality members

		public bool Equals(Host other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other._machineName, _machineName);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (Host)) return false;
			return Equals((Host) obj);
		}

		public override int GetHashCode()
		{
			return (_machineName != null ? _machineName.GetHashCode() : 0);
		}

		#endregion
	}
}
