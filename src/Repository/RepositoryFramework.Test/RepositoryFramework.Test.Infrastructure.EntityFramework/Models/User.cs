using System;
using System.Collections.Generic;

namespace RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal
{
    public partial class User
    {
        public User()
        {
            IdGruppos = new HashSet<Group>();
        }

        public int Identificativo { get; set; }
        public string Nome { get; set; } = null!;
        public string IndirizzoElettronico { get; set; } = null!;
        public string Cognome { get; set; } = null!;

        public virtual ICollection<Group> IdGruppos { get; set; }
    }
}
