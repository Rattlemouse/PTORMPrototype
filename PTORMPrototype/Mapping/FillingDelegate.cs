using System.Data;

namespace PTORMPrototype.Mapping
{
    public delegate void FillingDelegate(IDataRecord r, object obj, int offset);
}