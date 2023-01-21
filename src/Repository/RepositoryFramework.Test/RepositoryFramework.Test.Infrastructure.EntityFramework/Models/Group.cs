﻿using System;
using System.Collections.Generic;

namespace RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal
{
    public partial class Group
    {
        public Group()
        {
            IdentificativoUsers = new HashSet<User>();
        }

        public int IdGruppo { get; set; }
        public string Nome { get; set; } = null!;
        public bool Visibile { get; set; }

        public virtual ICollection<User> IdentificativoUsers { get; set; }
    }
}
