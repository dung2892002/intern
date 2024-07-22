﻿namespace BE__Back_End_.Models
{
    public class Position
    {
        public Guid PositionId { get; set; }

        public string PositionName { get; set; }

        public string PositionCode { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }
    }
}
