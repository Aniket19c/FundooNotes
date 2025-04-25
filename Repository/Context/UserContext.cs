using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Models.Entity;
using Repository.Entity;


namespace Repository.Context
{
    public  class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> option) :base (option){
        
        }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<NotesEntity> Notes { get; set; }
        public DbSet<CollaboratorEntity> Collaborators { get; set; }
        public DbSet<LabelEntity> Labels { get; set; }


    }
}
