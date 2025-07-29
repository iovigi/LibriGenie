namespace LibriGenie.Workers.Services.Models;

public class DataItem<T>
{
    public string Id { get; set; } = string.Empty;
    public T Data { get; set; } = default(T)!;
} 