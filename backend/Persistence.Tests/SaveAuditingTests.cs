using Core.Entities;
using Core.Exceptions;

namespace Persistence.Tests
{
    public class SaveAuditingTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private static Profile NewProfile() => new() { FirstName = "Anna", LastName = "Schmidt", Email = "anna@example.com" };

        [Fact]
        public async Task New_entity_gets_creation_time_on_save()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = NewProfile();
            await uow.Profiles.AddAsync(profile);

            var before = DateTime.UtcNow;
            await uow.SaveChangesAsync();
            var after = DateTime.UtcNow;

            var loaded = (await _db.CreateUnitOfWork().Profiles.GetByIdAsync(profile.Id))!;
            Assert.InRange(loaded.CreatedAt, before, after);
            Assert.Equal(loaded.CreatedAt, loaded.UpdatedAt);
        }

        [Fact]
        public async Task Edited_entity_gets_new_update_time_but_keeps_creation_time()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = NewProfile();
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();
            var createdAt = profile.CreatedAt;

            var edit = _db.CreateUnitOfWork();
            (await edit.Profiles.GetByIdAsync(profile.Id))!.City = "Berlin";
            var before = DateTime.UtcNow;
            await edit.SaveChangesAsync();
            var after = DateTime.UtcNow;

            var loaded = (await _db.CreateUnitOfWork().Profiles.GetByIdAsync(profile.Id))!;
            Assert.Equal(createdAt, loaded.CreatedAt);
            Assert.InRange(loaded.UpdatedAt, before, after);
        }

        [Fact]
        public async Task Saving_a_stale_copy_after_someone_else_saved_is_rejected()
        {
            var uow = _db.CreateUnitOfWork();
            var profile = NewProfile();
            await uow.Profiles.AddAsync(profile);
            await uow.SaveChangesAsync();

            var user = _db.CreateUnitOfWork();
            var worker = _db.CreateUnitOfWork();
            (await user.Profiles.GetByIdAsync(profile.Id))!.City = "Berlin";
            (await worker.Profiles.GetByIdAsync(profile.Id))!.City = "Hamburg";
            await worker.SaveChangesAsync();

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => user.SaveChangesAsync());
            Assert.Equal("Hamburg", (await _db.CreateUnitOfWork().Profiles.GetByIdAsync(profile.Id))!.City);
        }
    }
}
