using System.Text.Json;

using Fpfl.Api.Mappers;
using Fpfl.Display.Domain;
using Fpfl.Display.Domain.Recurrence;
using Fpfl.Items.Domain;
using Fpfl.Items.Domain.Entities;

namespace Fpfl.Api.Tests;

public sealed class DisplayMapperTests
{
    [Fact]
    public void Serializes_in_the_same_shape_as_api_jdk()
    {
        var debit = new Item
        {
            Id = 7,
            ItemTypeId = ItemRules.Debit,
            ItemType = new ItemType { Id = 2, Name = "Debit" },
            TimePeriodId = ItemRules.Daily,
            TimePeriod = new TimePeriod { Id = 2, Name = "Daily" },
            Name = "Coffee",
            Amount = 4.5,
        };
        var days = LedgerBuilder.Build([debit], new DateWindow(new DateOnly(2026, 10, 17), new DateOnly(2026, 10, 18)));

        var json = JsonSerializer.SerializeToElement(new DisplayMapper().ToResponse(days), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var day = json[1];
        Assert.Equal(2, day.GetProperty("rollupKey").GetInt32());
        Assert.Equal(2026, day.GetProperty("year").GetInt32());
        Assert.Equal("2026-10-18", day.GetProperty("wDate").GetString());
        Assert.Equal(0m, day.GetProperty("creditSummary").GetDecimal());
        Assert.Equal(-4.5m, day.GetProperty("debitSummary").GetDecimal());
        Assert.Equal(-4.5m, day.GetProperty("net").GetDecimal());
        Assert.Equal(-9m, day.GetProperty("runningTotal").GetDecimal());

        var item = day.GetProperty("items")[0];
        Assert.Equal(1, item.GetProperty("itemKey").GetInt32());
        Assert.Equal(7, item.GetProperty("itemId").GetInt64());
        Assert.Equal(2, item.GetProperty("rollupKey").GetInt32());
        Assert.Equal("2026-10-18", item.GetProperty("occurrenceDate").GetString());
        Assert.Equal(2, item.GetProperty("fkItemType").GetInt64());
        Assert.Equal("Debit", item.GetProperty("itemType").GetString());
        Assert.Equal("Daily", item.GetProperty("period").GetString());
        Assert.Equal("Coffee", item.GetProperty("name").GetString());
        Assert.Equal(-4.5m, item.GetProperty("amount").GetDecimal());
    }
}
