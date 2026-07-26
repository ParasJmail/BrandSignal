namespace BrandSignal.Application.Common.Models;

public class PaginatedList<T>{
    public List<T> Items {get; set;}
    public int PageNumber {get; set;}
    public int TotalPages {get; set;}
    public int TotalCount {get; set;}
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}