using System;

namespace Repository.Helper.CustomExceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string message = "User already exists.") : base(message)
        {
        }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message = "User not found.") : base(message)
        {
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message = "A database error occurred.") : base(message)
        {
        }
    }
}
