namespace Mozzi.Data
{
    // JSON 한 줄이 되는 행. DataTable 이 이 Id 로 찾는다.
    public interface IDataRow
    {
        string Id { get; }
    }
}
