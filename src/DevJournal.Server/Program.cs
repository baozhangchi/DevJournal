#region

using System.Text.Json.Serialization;

#endregion

StaticConfig.EnableAot = true;
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var dbFactory = () =>
{
    var db = new SqlSugarClient(new ConnectionConfig()
    {
        IsAutoCloseConnection = true,
        DbType = DbType.Sqlite,
        ConnectionString = $"datasource=journal.db"
    }, c =>
    {
        c.Aop.OnLogExecuting = (sql, parameters) =>
        {
            Console.WriteLine(UtilMethods.GetNativeSql(sql, parameters));
        };
    });
    return db;
};

var app = builder.Build();
app.SetMap();

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}