using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests;

public sealed class DatabaseModelTests
{
    [Fact]
    public void SqlServerModelMatchesPartOneTableNamesAndColumnTypes()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseSqlServer("Server=localhost;Database=RaceDayMetadataOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new RaceDayContext(options);
        var model = context.Model;

        var expectedTables = new[] { "Roles", "Users", "Events", "Categories", "Enrolments", "Results" };
        Assert.Equal(
            expectedTables.OrderBy(name => name),
            model.GetEntityTypes().Select(entity => entity.GetTableName()).OrderBy(name => name));

        Assert.Equal("varchar(20)", ColumnType<Role>(context, nameof(Role.RoleName)));
        Assert.Equal("varchar(100)", ColumnType<User>(context, nameof(User.FullName)));
        Assert.Equal("varchar(150)", ColumnType<User>(context, nameof(User.Email)));
        Assert.Equal("varchar(255)", ColumnType<User>(context, nameof(User.PasswordHash)));
        Assert.Equal("datetime", ColumnType<Event>(context, nameof(Event.EventDate)));
        Assert.Equal("decimal(6,2)", ColumnType<Event>(context, nameof(Event.EventDistanceKm)));
        Assert.Equal("varchar(10)", ColumnType<Event>(context, nameof(Event.EventType)));
        Assert.Equal("decimal(5,2)", ColumnType<Category>(context, nameof(Category.DistanceKm)));
        Assert.Equal("time", ColumnType<Result>(context, nameof(Result.FinishTime)));
    }

    [Fact]
    public void SqlServerModelKeepsPartOneUniquenessAndNoActionRelationships()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseSqlServer("Server=localhost;Database=RaceDayMetadataOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        using var context = new RaceDayContext(options);
        var model = context.Model;

        Assert.Contains(model.FindEntityType(typeof(Role))!.GetIndexes(),
            index => index.IsUnique && index.Properties.Single().Name == nameof(Role.RoleName));
        Assert.Contains(model.FindEntityType(typeof(User))!.GetIndexes(),
            index => index.IsUnique && index.Properties.Single().Name == nameof(User.Email));
        Assert.Contains(model.FindEntityType(typeof(Enrolment))!.GetIndexes(),
            index => index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Enrolment.ParticipantId), nameof(Enrolment.CategoryId) }));
        Assert.Contains(model.FindEntityType(typeof(Result))!.GetIndexes(),
            index => index.IsUnique && index.Properties.Single().Name == nameof(Result.EnrolmentId));

        Assert.All(model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private static string? ColumnType<TEntity>(RaceDayContext context, string propertyName)
        where TEntity : class =>
        context.Model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName)!.GetColumnType();
}
