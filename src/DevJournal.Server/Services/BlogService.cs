using DevJournal.Server.Entities;

namespace DevJournal.Server.Services;

[Group("blog")]
public class BlogService
{
    [Get("/all_categories")]
    public List<Category> GetAllCategories()
    {
        return new List<Category>();
    }
}