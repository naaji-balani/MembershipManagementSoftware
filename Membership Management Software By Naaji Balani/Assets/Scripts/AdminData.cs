using Firebase.Firestore;

[FirestoreData]

public struct AdminData
{
    [FirestoreProperty]
    public string Number { get; set; }


    [FirestoreProperty]
    public string IsAdmin { get; set; }


    [FirestoreProperty]
    public string CanWrite { get; set; }


    [FirestoreProperty]
    public string IsAllowed { get; set; }
}
