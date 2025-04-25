﻿namespace Repository.DTO
{
    public class ResponseDto<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public T? data { get; set; } 
    }
}
