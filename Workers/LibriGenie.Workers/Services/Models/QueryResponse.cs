namespace LibriGenie.Workers.Services.Models;

public class QueryResponse<T>
{
    public List<DataItem<T>> Data { get; set; } = new List<DataItem<T>>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int TotalPages { get; set; }
} 