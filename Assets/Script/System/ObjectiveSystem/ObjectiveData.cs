using System;

[Serializable]
public class ObjectiveData
{
    public string ID;
    public string Description;
    public bool IsCompleted;

    public ObjectiveData(string id, string description)
    {
        ID = id;
        Description = description;
        IsCompleted = false;
    }
}