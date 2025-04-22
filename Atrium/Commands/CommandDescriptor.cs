using System;
using System.Collections.Generic;

namespace Atrium
{
    public struct CommandDescriptor : IEquatable<CommandDescriptor>
    {
        public static readonly IEqualityComparer<CommandDescriptor> Comparer = new EQComparer();

        public readonly string Name;
        public readonly string DisplayName;
        public readonly string ShortDescription;
        public readonly string LongDescription;

        public CommandDescriptor(string name, string displayName, string? shortDescription = null, string? longDescription = null) : this(name, displayName)
        {
            this.ShortDescription = shortDescription ?? string.Empty;
            this.LongDescription = longDescription ?? string.Empty;
        }

        public CommandDescriptor(string name, string displayName)
        {
            this.Name = name;
            this.DisplayName = displayName ?? name;
            this.ShortDescription = string.Empty;
            this.LongDescription = string.Empty;
        }

        public override readonly string ToString() => String.Format("Command '{0}'", this.DisplayName);

        //--------------------------------------------------------------//
        #region Conversion From/To String
        public static implicit operator CommandDescriptor(string s)
        {
            return new CommandDescriptor(s, s, null, null);
        }
        public static implicit operator string(CommandDescriptor s)
        {
            return s.Name;
        }

        public static implicit operator CommandDescriptor((string name, string display) s)
        {
            return new CommandDescriptor(s.name, s.display, null, null);
        }
        public static implicit operator CommandDescriptor((string name, string display, string ShortDescription) s)
        {
            return new CommandDescriptor(s.name, s.display, s.ShortDescription, null);
        }

        public static implicit operator CommandDescriptor((string name, string display, string ShortDescription, string LongDescription) s)
        {
            return new CommandDescriptor(s.name, s.display, s.ShortDescription, s.LongDescription);
        }
        #endregion
        //--------------------------------------------------------------//
        #region Equality Check
        public static bool operator ==(CommandDescriptor descriptor1, CommandDescriptor descriptor2)
        {
            return descriptor1.Equals(descriptor2);
        }

        public static bool operator !=(CommandDescriptor descriptor1, CommandDescriptor descriptor2)
        {
            return !(descriptor1 == descriptor2);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandDescriptor && this.Equals((CommandDescriptor)obj);
        }

        public bool Equals(CommandDescriptor other)
        {
            return this.Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return 539060726 + StringComparer.InvariantCultureIgnoreCase.GetHashCode(this.Name);
        }
        #endregion
        //--------------------------------------------------------------//
        private class EQComparer : IEqualityComparer<CommandDescriptor>
        {
            bool IEqualityComparer<CommandDescriptor>.Equals(CommandDescriptor x, CommandDescriptor y)
            {
                return x.Equals(y);
            }

            int IEqualityComparer<CommandDescriptor>.GetHashCode(CommandDescriptor obj)
            {
                return obj.GetHashCode();
            }
        }
        //--------------------------------------------------------------//
    }
}
