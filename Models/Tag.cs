﻿using Microsoft.EntityFrameworkCore;

namespace BlogReview.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual List<Article> Articles { get; set; } = new();
    }
}
