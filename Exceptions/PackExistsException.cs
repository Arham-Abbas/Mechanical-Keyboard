using System;

namespace Mechanical_Keyboard.Exceptions
{
    public class PackExistsException(string packName) : Exception($"A sound pack named '{packName}' already exists.")
    {
        public string PackName { get; } = packName;
    }
}
