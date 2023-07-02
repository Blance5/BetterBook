using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinalBookProj.Models
{
    public class SearchViewModel
    {
        public string Query { get; set; }
        public List<Book> SearchResults { get; set; }
        public int CurrentPage { get; set; }
        public List<string> userList { get; set; }
        public int TotalPages { get; set; }
    }
    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public double Rating { get; set; }
        public string ID { get; set; }
        public int pageCount { get; set; }
        public string Img { get; set; } 
        public string Description { get; set; } 
        public string Preview { get; set; }
    }

    public class UBITable
    {
        [Key] public int id { get; set; }
        public string userID { get; set; }
        public string bookID { get; set; }
    }

    public class user
    {
        public List<string> userList { get; set; }
    }
}