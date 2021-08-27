using System.Collections;

namespace ERX.Services.Helpers.Message
{
  public interface IMessage
  {
    void AddEntity(ArrayList entityList);

    void AddEntity(object entity);

    void ResetEntityList();

    string Content { get; set; }

    ArrayList EntityList { get; set; }

    int MessageID { get; set; }

    bool Success { get; set; }
  }
}
