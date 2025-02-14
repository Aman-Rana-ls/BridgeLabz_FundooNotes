using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RepoLayer.Context;
using RepoLayer.Entity;
using RepoLayer.Interfaces;
using ModelLayer;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace RepoLayer.Services
{
    public class NoteRL : INoteRL
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteRL> _logger;
        private readonly IDatabase _cache;

        public NoteRL(ApplicationDbContext context, ILogger<NoteRL> logger, IConnectionMultiplexer redis)
        {
            _context = context;
            _logger = logger;
            _cache = redis.GetDatabase();
        }

        private async Task UpdateCacheAsync(string cacheKey, Note note)
        {
            await _cache.ListRightPushAsync(cacheKey, JsonSerializer.Serialize(note));
        }

        private async Task<List<Note>> GetCachedNotesAsync(string cacheKey)
        {
            var cachedNotes = await _cache.ListRangeAsync(cacheKey);
            return cachedNotes.Select(n => JsonSerializer.Deserialize<Note>(n)).ToList();
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            var cacheKey = $"user:{note.CreatedBy}:notes";
            var cachedNotes = await _cache.ListRangeAsync(cacheKey);

            
            if (cachedNotes.Any())
            {
                await UpdateCacheAsync(cacheKey, note);
            }

            return note;
        }


        public async Task<List<Note>> GetNotesByUserAsync(int userId)
        {
            var cacheKey = $"user:{userId}:notes";
            var cachedNotes = await GetCachedNotesAsync(cacheKey);

            if (cachedNotes.Any())
                return cachedNotes;

            var notes = await _context.Notes.Where(n => n.CreatedBy == userId && !n.IsDeleted && !n.IsArchived).ToListAsync();
            foreach (var note in notes)
                await UpdateCacheAsync(cacheKey, note);

            return notes;
        }

        public async Task<List<Note>> GetArchiveNotes(int userId)
        {
            var cacheKey = $"user:{userId}:archivedNotes";
            var cachedNotes = await GetCachedNotesAsync(cacheKey);

            if (cachedNotes.Any())
                return cachedNotes;

            var notes = await _context.Notes.Where(n => n.CreatedBy == userId && n.IsArchived).ToListAsync();
            foreach (var note in notes)
                await UpdateCacheAsync(cacheKey, note);

            return notes;
        }

        public async Task<List<Note>> GetNotesFromBin(int userId)
        {
            var cacheKey = $"user:{userId}:binNotes";
            var cachedNotes = await GetCachedNotesAsync(cacheKey);

            if (cachedNotes.Any())
                return cachedNotes;

            var notes = await _context.Notes.Where(n => n.CreatedBy == userId && n.IsDeleted).ToListAsync();
            foreach (var note in notes)
                await UpdateCacheAsync(cacheKey, note);

            return notes;
        }

        public async Task<Note> GetNoteByIdAsync(int noteId)
        {
            return await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId);
        }

        public async Task<Note> UpdateNoteAsync(UpdateNote note, int noteId, int userId)
        {
            var existingNote = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
            if (existingNote == null) return null;

            existingNote.Title = note.Title;
            existingNote.Description = note.Description;
            existingNote.Color = note.Color;
            existingNote.IsDeleted = note.IsDeleted;
            existingNote.IsArchived = note.IsArchived;

            await _context.SaveChangesAsync();

            var cacheKey = $"user:{userId}:notes";
            var cachedNotes = await _cache.ListRangeAsync(cacheKey);
            foreach (var cachedNote in cachedNotes)
            {
                var deserializedNote = JsonSerializer.Deserialize<Note>(cachedNote);
                if (deserializedNote.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(cacheKey, cachedNote);
                    break;
                }
            }
            await UpdateCacheAsync(cacheKey, existingNote);

            return existingNote;
        }

        public async Task<Note> ArchiveNoteAsync(int noteId, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
            if (note == null) return null;

            note.IsArchived = true;
            await _context.SaveChangesAsync();

            var cacheKey = $"user:{userId}:notes";
            var cachedNotes = await _cache.ListRangeAsync(cacheKey);


            foreach (var cachedNote in cachedNotes)
            {
                var deserializedNote = JsonSerializer.Deserialize<Note>(cachedNote);
                if (deserializedNote.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(cacheKey, cachedNote);
                    break;
                }
            }

            var serializedNote = JsonSerializer.Serialize(note);
            var archivedCacheKey = $"user:{userId}:archivedNotes";

            var existingNotes = await _cache.ListRangeAsync(archivedCacheKey);

           
            if (existingNotes.Any())
            {
                
                await _cache.ListRightPushAsync(archivedCacheKey, serializedNote);
            }


            return note;
        }


        public async Task<bool> UnArchiveNoteAsync(int noteId, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId && n.IsArchived);
            if (note == null) return false;

            note.IsArchived = false;
            await _context.SaveChangesAsync();

            var cacheKey = $"user:{userId}:notes";
            var archivedCacheKey = $"user:{userId}:archivedNotes";

            var serializedNote = JsonSerializer.Serialize(note);
            await _cache.ListRightPushAsync(cacheKey, serializedNote); // Move note back to active notes

            var archivedNotes = await _cache.ListRangeAsync(archivedCacheKey);

            foreach (var cachedNote in archivedNotes)
            {
                var deserializedNote = JsonSerializer.Deserialize<Note>(cachedNote);
                if (deserializedNote.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(archivedCacheKey, cachedNote);
                    break;
                }
            }

            return true;
        }

        public async Task<bool> DeleteNoteAsync(int noteId, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
            if (note == null) return false;

            note.IsDeleted = true;
            await _context.SaveChangesAsync();

            var cacheKey = $"user:{userId}:notes";
            var cachedNotes = await _cache.ListRangeAsync(cacheKey);

            foreach (var cachedNote in cachedNotes)
            {
                var deserializedNote = JsonSerializer.Deserialize<Note>(cachedNote);
                if (deserializedNote.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(cacheKey, cachedNote);
                    break;
                }
            }

            var serializedNote = JsonSerializer.Serialize(note);
            var binCacheKey = $"user:{userId}:binNotes";
            var existingBinNotes = await _cache.ListRangeAsync(binCacheKey);

            if (existingBinNotes.Any())
            {
               
                await _cache.ListRightPushAsync(binCacheKey, serializedNote);
            }


            return true;
        }
        public async Task<bool> RestoreFromBin(int noteId, int userId)
        {
            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId && n.IsDeleted);
            if (note == null) return false;
            note.IsDeleted = false;
            await _context.SaveChangesAsync();

            var serializedNote = JsonSerializer.Serialize(note);

            var binCacheKey = $"user:{userId}:binNotes";
            var deletedNotes = await _cache.ListRangeAsync(binCacheKey);

            foreach (var deletedNote in deletedNotes)
            {
                var deserializedNote = JsonSerializer.Deserialize<Note>(deletedNote);
                if (deserializedNote.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(binCacheKey, deletedNote);
                    break;
                }
            }
            var notesCacheKey = $"user:{userId}:notes";
            await _cache.ListRightPushAsync(notesCacheKey, serializedNote);

            return true;
        }
        public async Task<bool> DeletePermNoteAsync(int noteId, int userId)
        {

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == noteId && n.CreatedBy == userId);
            if (note == null) return false;

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            var binCacheKey = $"user:{userId}:binNotes";

            var binCachedNotes = await _cache.ListRangeAsync(binCacheKey);
            foreach (var binCachedNote in binCachedNotes)
            {
                var noteJson = binCachedNote.ToString();
                var cachedNote = JsonSerializer.Deserialize<Note>(noteJson);

                if (cachedNote?.NoteId == noteId)
                {
                    await _cache.ListRemoveAsync(binCacheKey, binCachedNote);
                    break;
                }
            }

            return true;
        }

    }
}
