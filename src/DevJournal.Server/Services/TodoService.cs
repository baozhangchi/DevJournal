namespace DevJournal.Server.Services;

[Group("todo")]
public class TodoService
{
    private readonly Todo[] _sampleTodos;

    public TodoService()
    {
        _sampleTodos = new Todo[]
        {
            new(1, "Walk the dog"),
            new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
            new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
            new(4, "Clean the bathroom"),
            new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
        };
    }

    [Get("/")]
    public Todo[] GetAll()
    {
        return _sampleTodos;
    }

    [Get("/{id}")]
    public IResult GetById(int id)
    {
        return _sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
            ? Results.Ok(todo)
            : Results.NotFound();
    }
}