using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Context;
using RepoLayer.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RepositoryLayer.Services
{
    public class CollaboratorRL : ICollaboratorRL
    {
        private readonly ApplicationDbContext _context;

        public CollaboratorRL(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddCollaborator(int noteId, string collaboratorEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == collaboratorEmail);
            if (user == null)
                return false;

            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
                return false;

            var collaborator = new NoteCollaborator
            {
                NoteId = noteId,
                UserId = user.Id
            };

            _context.NoteCollaborators.Add(collaborator);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCollaborator(int noteId, string collaboratorEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == collaboratorEmail);
            if (user == null)
                return false;

            var collaborator = await _context.NoteCollaborators
                .FirstOrDefaultAsync(nc => nc.NoteId == noteId && nc.UserId == user.Id);

            if (collaborator == null)
                return false;

            _context.NoteCollaborators.Remove(collaborator);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetCollaboratorsByNoteId(int noteId)
        {
            var collaborators = await _context.NoteCollaborators
                .Where(nc => nc.NoteId == noteId)
                .Include(nc => nc.User)
                .Select(nc => nc.User.Email)
                .ToListAsync();

            return collaborators;
        }
    }
}