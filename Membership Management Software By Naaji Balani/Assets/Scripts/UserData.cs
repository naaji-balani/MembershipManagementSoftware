using Firebase.Firestore;

[FirestoreData]

public struct UserData
{
    [FirestoreProperty]
    public string Name { get; set; }


    [FirestoreProperty]
    public string Number { get; set; }


    [FirestoreProperty]
    public string SubscriptionExpiryDate { get; set; }


    [FirestoreProperty]
    public string MembershipType { get; set; }


    [FirestoreProperty]
    public string History { get; set; }


    [FirestoreProperty]
    public string ActivationDate { get; set; }


    [FirestoreProperty]
    public string Amount { get; set; }


    [FirestoreProperty]
    public string PaymentDate { get; set; }


    [FirestoreProperty]
    public string PaymentStatus { get; set; }


    [FirestoreProperty]
    public string BalanceAmount { get; set; }


    [FirestoreProperty]
    public string GstAmount { get; set; }


    [FirestoreProperty]
    public string BaseAmount { get; set; }
}
