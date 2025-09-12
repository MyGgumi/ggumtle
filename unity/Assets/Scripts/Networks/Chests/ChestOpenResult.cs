using System.Collections.Generic;

namespace Networks.chests
{
    public class ChestOpenResult
    {
        public List<int> Items { get; set; }
        
        public ChestOpenResult(List<int> items)
        {
            Items = items;
        }
    }
}