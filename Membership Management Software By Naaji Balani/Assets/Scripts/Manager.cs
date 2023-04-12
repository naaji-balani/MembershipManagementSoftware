using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Globalization;
using System.IO;
using System.Net;
using System.Collections;

public class Manager : MonoBehaviour
{
    [SerializeField] TMP_InputField _nameField, _addNumberField, _dateField, _monthField, _yearField, _checkNumberField,_dateFieldInRange,_monthFieldInRange,_yearFieldInRange,_numberInputFieldLogin,_otpInputField,_activationDateInputfield,_activationMonthInputfield,_activationYearInputField,_netAmountField,_balanceAmountInputField,_gstAmountInputField,_baseAmountInputField;
    [SerializeField] string _date , _activationDate,_amount,_paymentStatus,_paymentDate,_balanceAmount;
    [SerializeField] GameObject _customDatePanel, _namePanelPrefab,_trialPanelPrefab,_numberPanel,_otpPanel,_activationDatesPanel,_balancePanelPrefab,_permissionPanel;
    [SerializeField] GameObject[] _panels;
    [SerializeField] TMP_InputField[] _allInputFields;
     [SerializeField] TextMeshProUGUI _savedText, _validNumberText,_validDateText,_numberText,_numberErrorText,_otpErrorText;
    [SerializeField] TMP_Dropdown _packageDropdown, _memberShipTypeDropdown,_paymentStatusDropdown;
    [SerializeField] List<string> _documents, _trialMembers,_rangeNames,_balanceNames,_enquiryMembers;
    [SerializeField] Transform _panelsContainer,_trialPanelContainer, _rangePanel,_balancePanelContainer, _addDetailsPanel,_enquiryPanelContainer;
    [SerializeField] string _randomNumber,_operator;
    [SerializeField] byte _isDateVerified;

    // Reference to the Firebase Firestore database
    FirebaseFirestore db;

    // Reference to the UserData object to be saved
    UserData userData;


    private void Awake()
    {
        if (PlayerPrefs.HasKey("Number"))
        {
            _numberPanel.SetActive(false);
            _panels[0].SetActive(true);
            _operator = PlayerPrefs.GetString("Number");
        }

    }

    void Start() => db = FirebaseFirestore.DefaultInstance;

    private void FixedUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Escape)) RestartScene();

    }

    public void OpenPanel(GameObject openPanel)
    {
        _panels[0].SetActive(false);
        openPanel.SetActive(true);
    }

    public async void CheckPermissionToRead(int i)
    {
        DocumentReference docRef = db.Collection("Operators").Document(_operator);

        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists && snapshot.ContainsField("IsAllowed"))
        {
            string isAllowedStr = snapshot.GetValue<string>("IsAllowed");

            if (isAllowedStr == "false")
            {
                _permissionPanel.SetActive(true);
                return;
            }

            if(i == 2)
            {
                if (_panelsContainer.childCount > 0)
                {
                    foreach (Transform child in _panelsContainer)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            OpenPanel(_panels[i]);

        }
    }

    public async void CheckPermissionToWrite(int i)
    {
        DocumentReference docRef = db.Collection("Operators").Document(_operator);

        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists && snapshot.ContainsField("IsAllowed") && snapshot.ContainsField("CanWrite"))
        {
            string isAllowedStr = snapshot.GetValue<string>("IsAllowed");
            string canWriteStr = snapshot.GetValue<string>("CanWrite");

            if(isAllowedStr == "false" || canWriteStr == "false")
            {
                _permissionPanel.SetActive(true);
                return;
            }

            OpenPanel(_panels[i]);

            if (i == 3) GetTrialMembers();
            if (i == 5) GetBalancePaymentMembers();
            if (i == 6) GetEnquiryMembers();
        }
    }

    public void OkayButton() => _permissionPanel.SetActive(false);

    public void GetBaseAmountFromGst()
    {
        float amount = int.Parse(_netAmountField.text);

        float baseAmount = Mathf.Round(amount * 100 / 118);
        float gstAmount = Mathf.Round(0.18f * baseAmount);

        _gstAmountInputField.text = gstAmount.ToString();
        _baseAmountInputField.text = baseAmount.ToString();
    }

    public void AddGstToAmount()
    {
        float amount = int.Parse(_baseAmountInputField.text);
        float gstAmount = Mathf.Round(0.18f * amount);
        float netAmount = amount + gstAmount;

        _gstAmountInputField.text = gstAmount.ToString();
        _netAmountField.text = netAmount.ToString();
    }

    public void PaymentStatusDropdown() => _balanceAmountInputField.gameObject.SetActive(_paymentStatusDropdown.options[_paymentStatusDropdown.value].text == "Pending");

    public void VerifyDate(TMP_InputField dateField, TMP_InputField monthField, TMP_InputField yearField)
    {
        if (string.IsNullOrEmpty(dateField.text) || string.IsNullOrEmpty(monthField.text) || string.IsNullOrEmpty(yearField.text))
        {
            _savedText.text = "Invalid Date";
            _savedText.color = Color.red;
            return;
        }

        if (int.TryParse(dateField.text, out int day)
            && int.TryParse(monthField.text, out int month)
            && int.TryParse(yearField.text, out int year))
        {
            // Check if the entered birthdate is a valid date
            if (DateTime.TryParseExact(day + "/" + month + "/" + year, "d/M/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime birthDate))
            {
                DateTime today = DateTime.Today;
                int age = today.Year - birthDate.Year;

                // Check if the entered birthdate is in the future
                if (birthDate < today)
                {
                    _savedText.text = "Date can't be from the past.";
                    _savedText.color = Color.red;
                    return;
                }
            }
            else
            {
                _savedText.text = "Invalid Date";
                _savedText.color = Color.red;
                return;
            }
        }
        _isDateVerified = 1;
    }

    public async void OnContinueButtonClicked()
    {
        if (string.IsNullOrEmpty(_nameField.text))
        {
            _savedText.text = "Name can't be empty";
            _savedText.color = Color.red;
            return;
        }
        else if (string.IsNullOrEmpty(_addNumberField.text))
        {
            _savedText.text = "Number can't be empty";
            _savedText.color = Color.red;
            return;
        }
        else if (_addNumberField.text.Length != 10)
        {
            _savedText.text = "Invalid Number";
            _savedText.color = Color.red;
            return;
        }

        string paymentDate = DateTime.Today.ToString("dd-MM-yyyy");

        if(_memberShipTypeDropdown.value != 4)
        {
            if (_packageDropdown.value < 4)
            {
                GetExpiryDateFromDropdown();
                _activationDate = paymentDate;
            }
            else
            {
                if (string.IsNullOrEmpty(_activationYearInputField.text) || string.IsNullOrEmpty(_activationMonthInputfield.text) || string.IsNullOrEmpty(_activationDateInputfield.text) || _activationDateInputfield.text.Length < 2 || _activationMonthInputfield.text.Length < 2 || _activationYearInputField.text.Length < 4)
                {
                    _savedText.text = "Invalid Date";
                    _savedText.color = Color.red;
                    return;
                }

                VerifyDate(_dateField, _monthField, _yearField);
                if (_isDateVerified == 0) return;

                _date = $"{_dateField.text}-{_monthField.text}-{_yearField.text}";
                _activationDate = $"{_activationDateInputfield.text}-{_activationMonthInputfield.text}-{_activationYearInputField.text}";
            }
        }

        DocumentReference docRef = db.Collection("Members").Document(_addNumberField.text);
        string history = null;

        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            history = snapshot.GetValue<string>("History");
            Debug.Log(history);

            // Add the new content to the history string, separated by a newline character
        }
        else
        {
            history = null;
        }


        // Set the user data and phone number
        userData = new UserData()
        {
            Name = _nameField.text,
            Number = _addNumberField.text,
            SubscriptionExpiryDate = _date,
            MembershipType = _memberShipTypeDropdown.options[_memberShipTypeDropdown.value].text,
            History = history,
            ActivationDate = _activationDate,
            Amount = _netAmountField.text,
            PaymentDate = paymentDate,
            PaymentStatus = _paymentStatusDropdown.options[_paymentStatusDropdown.value].text,
            BalanceAmount = _balanceAmountInputField.text,
            GstAmount = _gstAmountInputField.text,
            BaseAmount = _baseAmountInputField.text
        };

        await SaveUserData(docRef, userData);


        UpdateHistory(_addNumberField.text);
        UpdateHistory("Updates");

        _savedText.text = "Member Added Successfully";
        _savedText.color = Color.green;
    }

    async Task SaveUserData(DocumentReference docRef, UserData userData)
    {
        try
        {
            await docRef.SetAsync(userData);
        }
        catch (Exception e)
        {
            // Failed to save data
            _savedText.text = "Failed to save user data to Firestore: " + e;
            _savedText.color = Color.red;
            return;
        }
    }

    void UpdateHistory(string document)
    {
        DocumentReference docRef = db.Collection("Members").Document(document);

        // Get the current value of the history field
        docRef.GetSnapshotAsync().ContinueWith(task =>
        {
            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                string history = snapshot.GetValue<string>("History");

                // Add the new content to the history string, separated by a newline character
                string newContent = "";
                if (_memberShipTypeDropdown.value != 4) newContent = $"{_addNumberField.text} paid {_netAmountField.text} for {_memberShipTypeDropdown.options[_memberShipTypeDropdown.value].text} taken on {DateTime.Now} starting from {_activationDate} expiring on {_date} by {_operator}.";
                else newContent = $"{_addNumberField.text} Enquired on {DateTime.Now} by {_operator}";

                string updatedHistory;

                if (history == null)
                {
                    updatedHistory = newContent;
                }
                else
                {
                    updatedHistory = history + "\n" + newContent + " .";
                }

                // Update the document with the updated history field
                docRef.UpdateAsync("History", updatedHistory).ContinueWith(updateTask =>
                {
                    if (updateTask.IsCompleted && !updateTask.IsCanceled && !updateTask.IsFaulted)
                    {
                        Debug.Log("Document updated successfully");
                    }
                    else
                    {
                        Debug.LogError("Error updating document: " + updateTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("Document not found");
            }
        });
    }


    public void GetExpiryDateFromDropdown()
    {
        DateTime currentDate = DateTime.Now;

        TimeSpan duration = TimeSpan.Zero;

        switch (_packageDropdown.value)
        {
            case 0:
                duration = TimeSpan.FromDays(365);
                break;
            case 1:
                duration = TimeSpan.FromDays(365 / 2);
                break;
            case 2:
                duration = TimeSpan.FromDays(365 / 4);
                break;
            case 3:
                duration = TimeSpan.FromDays(30);
                break;
            case 4:
                break;

        }

        _customDatePanel.SetActive(_packageDropdown.value == 4);
        _activationDatesPanel.SetActive(_packageDropdown.value == 4);

        DateTime expiryDate = currentDate + duration;
        _date = expiryDate.ToString("dd-MM-yyy");

    }

    public static async Task<bool> IsUserExistAsync(string number)
    {
        // Get a reference to the collection
        CollectionReference collectionRef = FirebaseFirestore.DefaultInstance.Collection("Members");

        // Get a reference to a specific document in the collection
        DocumentReference documentRef = collectionRef.Document(number);

        // Check if the document exists
        DocumentSnapshot documentSnapshot = await documentRef.GetSnapshotAsync().ConfigureAwait(false);

        // Return a boolean indicating whether or not the document exists
        return documentSnapshot.Exists;
    }

    public void SearchDocuments(string searchQuery, string field, Action<List<string>> callback)
    {
        // Create a query that searches for documents that match the searchQuery in the specified field
        Query query = db.Collection("Members");

        // Execute the query and retrieve the matching documents
        query.GetSnapshotAsync().ContinueWith(task =>
        {
            // Check if the task completed successfully
            if (task.IsCompletedSuccessfully)
            {
                // Retrieve the QuerySnapshot from the completed task
                QuerySnapshot snapshot = task.Result;

                // Loop through the documents in the QuerySnapshot and add the names of the matching documents to the list
                List<string> documents = new List<string>();
                foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
                {
                    string documentName = documentSnapshot.Id;
                    Dictionary<string, object> data = documentSnapshot.ToDictionary();

                    // Check if the specified field contains the search query
                    if (data.ContainsKey(field) && data[field].ToString().ToLowerInvariant().Contains(searchQuery.ToLowerInvariant()))
                    {
                        // Add the document ID to the list of documents
                        documents.Add(documentName);
                    }
                }

                // Call the callback function with the list of document names
                callback(documents);
            }
            else
            {
                // Handle any errors that occurred
                Debug.LogError("Failed to get documents: " + task.Exception);
            }
        });
    }


    public async void CheckUser()
    {
        _documents.Clear();

        if(_panelsContainer.childCount > 0)
        {
            foreach(Transform child in _panelsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (string.IsNullOrEmpty(_checkNumberField.text)) return;


        // Use Task.WhenAll to wait for both SearchDocuments calls to complete
        await Task.WhenAll(
            SearchDocumentsAsync(_checkNumberField.text, "Name", documents =>
            {
            // Handle the list of document names
            foreach (string documentName in documents)
                {
                    _documents.Add(documentName);
                }
            }),
            SearchDocumentsAsync(_checkNumberField.text, "Number", documents =>
            {
            // Handle the list of document names
            foreach (string documentName in documents)
                {
                    _documents.Add(documentName);
                }
            })
        );

        if (_documents.Count > 0)
        {

            for (int i = 0; i < _documents.Count; i++)
            {
                // Get a reference to the document you want to retrieve
                DocumentReference docRef = db.Collection("Members").Document(_documents[i]);

                // Get the document snapshot asynchronously
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                string name = null, number = null, date = null;
                // Check if the snapshot contains data
                if (snapshot.Exists)
                {
                    // Get the values of the "Name", "Number", and "Date" fields as strings
                    name = snapshot.GetValue<string>("Name");
                    number = snapshot.GetValue<string>("Number");
                    date = snapshot.GetValue<string>("SubscriptionExpiryDate");
                }
                else
                {
                    Debug.Log("Document does not exist!");
                }
                GameObject namePanel = Instantiate(_namePanelPrefab, default);

                namePanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = name;
                namePanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = number;
                namePanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = date;
 

                namePanel.transform.parent = _panelsContainer;
            }
        }
    }

    async Task SearchDocumentsAsync(string searchQuery, string field, Action<List<string>> callback)
    {
        // Create a query that searches for documents that match the searchQuery in the specified field
        Query query = db.Collection("Members");

        // Execute the query and retrieve the matching documents
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        // Loop through the documents in the QuerySnapshot and add the names of the matching documents to the list
        List<string> documents = new List<string>();
        foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
        {
            string documentName = documentSnapshot.Id;
            Dictionary<string, object> data = documentSnapshot.ToDictionary();

            // Check if the specified field contains the search query
            if (data.ContainsKey(field) && data[field].ToString().ToLowerInvariant().Contains(searchQuery.ToLowerInvariant()))
            {
                // Add the document ID to the list of documents
                documents.Add(documentName);
            }
        }

        // Call the callback function with the list of document names
        callback(documents);
    }

    public async Task<List<string>> SearchDocumentsByMembershipType(string value,string field)
    {
        // Create a query that filters documents based on the MembershipType field
        Query query = db.Collection("Members").WhereEqualTo(field, value);

        // Execute the query and retrieve the matching documents
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        // Loop through the documents in the QuerySnapshot and add the IDs of the matching documents to the list
        List<string> documentIds = new List<string>();
        foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
        {
            string documentId = documentSnapshot.Id;
            documentIds.Add(documentId);
        }

        return documentIds;
    }

    public async void GetTrialMembers()
    {
        _trialMembers.Clear();
        _trialMembers = await SearchDocumentsByMembershipType("Trial", "MembershipType");

        if (_trialMembers.Count > 0)
        {
            for (int i = 0; i < _trialMembers.Count; i++)
            {
                // Get a reference to the document you want to retrieve
                DocumentReference docRef = db.Collection("Members").Document(_trialMembers[i]);

                // Get the document snapshot asynchronously
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                string name = null, number = null, date = null;

                // Check if the snapshot contains data
                if (snapshot.Exists)
                {
                    // Get the values of the "Name", "Number", and "Date" fields as strings
                    name = snapshot.GetValue<string>("Name");
                    number = snapshot.GetValue<string>("Number");
                    date = snapshot.GetValue<string>("SubscriptionExpiryDate");
                }
                else
                {
                    Debug.Log("Document does not exist!");
                }

                GameObject namePanel = Instantiate(_trialPanelPrefab, default);

                namePanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = name;
                namePanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = number;
                namePanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = date;


                namePanel.transform.parent = _trialPanelContainer;
            }
        }
    }

    public async void GetEnquiryMembers()
    {
        _enquiryMembers.Clear();
        _enquiryMembers = await SearchDocumentsByMembershipType("Enquiry", "MembershipType");

        if (_enquiryMembers.Count > 0)
        {
            for (int i = 0; i < _enquiryMembers.Count; i++)
            {
                // Get a reference to the document you want to retrieve
                DocumentReference docRef = db.Collection("Members").Document(_enquiryMembers[i]);

                // Get the document snapshot asynchronously
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                string name = null, number = null, date = null;

                // Check if the snapshot contains data
                if (snapshot.Exists)
                {
                    // Get the values of the "Name", "Number", and "Date" fields as strings
                    name = snapshot.GetValue<string>("Name");
                    number = snapshot.GetValue<string>("Number");
                    date = snapshot.GetValue<string>("PaymentDate");
                }
                else
                {
                    Debug.Log("Document does not exist!");
                }

                GameObject namePanel = Instantiate(_namePanelPrefab, default);

                namePanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = name;
                namePanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = number;
                namePanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = date;


                namePanel.transform.parent = _enquiryPanelContainer;
            }
        }
    }

    public async void GetBalancePaymentMembers()
    {
        _balanceNames.Clear();
        _balanceNames = await SearchDocumentsByMembershipType("Pending","PaymentStatus");

        if (_balanceNames.Count > 0)
        {
            for (int i = 0; i < _balanceNames.Count; i++)
            {
                // Get a reference to the document you want to retrieve
                DocumentReference docRef = db.Collection("Members").Document(_balanceNames[i]);

                // Get the document snapshot asynchronously
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                string name = null, number = null, date = null,balance = null;

                // Check if the snapshot contains data
                if (snapshot.Exists)
                {
                    // Get the values of the "Name", "Number", and "Date" fields as strings
                    name = snapshot.GetValue<string>("Name");
                    number = snapshot.GetValue<string>("Number");
                    date = snapshot.GetValue<string>("SubscriptionExpiryDate");
                    balance = snapshot.GetValue<string>("BalanceAmount");
                }
                else
                {
                    Debug.Log("Document does not exist!");
                }

                GameObject namePanel = Instantiate(_balancePanelPrefab, default);

                namePanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = name;
                namePanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = number;
                namePanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = date;
                namePanel.transform.GetChild(3).gameObject.GetComponent<TextMeshProUGUI>().text = "Balance : " + balance;


                namePanel.transform.parent = _balancePanelContainer;
            }
        }
    }

    public async void ConvertButton(GameObject panel)
    {
        DocumentReference docRef = db.Collection("Operators").Document(_operator);

        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists && snapshot.ContainsField("IsAllowed") && snapshot.ContainsField("CanWrite"))
        {
            string isAllowedStr = snapshot.GetValue<string>("IsAllowed");
            string canWriteStr = snapshot.GetValue<string>("CanWrite");

            if (isAllowedStr == "false" || canWriteStr == "false")
            {
                _permissionPanel.SetActive(true);
                return;
            }
        }

        int activePanelNumber = GetActivePanelIndex();

        _panels[activePanelNumber].SetActive(false);
        _panels[1].SetActive(true);

        string name = panel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text;
        string number = panel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text;

        _addDetailsPanel.GetChild(0).gameObject.GetComponent<TMP_InputField>().text = name;
        _addDetailsPanel.GetChild(1).gameObject.GetComponent<TMP_InputField>().text = number;

    }

    public void ClearButton(GameObject panel) => StartCoroutine(ClearBalance(panel));

    IEnumerator ClearBalance(GameObject panel )
    {
        string number = panel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text;
        DocumentReference docRef = db.Collection("Members").Document(number);
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "BalanceAmount", "" },
            {"PaymentStatus","Full Paid" }
         };
        Task updateTask = docRef.UpdateAsync(data);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        int index = panel.transform.GetSiblingIndex();
        Destroy(_balancePanelContainer.GetChild(index).gameObject);
    }

    public async void GetExpiringSubscriptions()
    {
        // Get today's date
        DateTime today = DateTime.Today;

        // Get the last day of the next month
        DateTime lastDayOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(2).AddDays(-1);

        // Create a query that filters documents based on the SubscriptionExpiryDate field
        Query query = db.Collection("Members").WhereGreaterThanOrEqualTo("SubscriptionExpiryDate", today.ToString("dd-MM-yyyy")).WhereLessThanOrEqualTo("SubscriptionExpiryDate", lastDayOfNextMonth.ToString("dd-MM-yyyy"));

        // Execute the query and retrieve the matching documents
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        // Loop through the documents in the QuerySnapshot and add the IDs of the matching documents to the list
        foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
        {
            string documentId = documentSnapshot.Id;
            _rangeNames.Add(documentId);
        }
    }

    public async void GetExpiringSubscriptionsByCustomDateRange()
    {
        _rangeNames.Clear();

        foreach(Transform child in _rangePanel)
        {
            Destroy(child.gameObject);
        }

        if (string.IsNullOrEmpty(_dateFieldInRange.text) || string.IsNullOrEmpty(_monthFieldInRange.text) || string.IsNullOrEmpty(_yearFieldInRange.text))
        {
            _validDateText.text = "Invalid Date";
            _validDateText.color = Color.red;
            return;
        }

        string day = _dateFieldInRange.text;
        string month = _monthFieldInRange.text;
        string year = _yearFieldInRange.text;

        // Parse the input fields to a DateTime object
        DateTime customDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day));

        // Get all documents in the Members collection
        Query query = db.Collection("Members");
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        // Loop through the documents in the QuerySnapshot and check if their SubscriptionExpiryDate is within the custom date range
        foreach (DocumentSnapshot documentSnapshot in snapshot.Documents)
        {
            // Check if the document has a SubscriptionExpiryDate field
            if (documentSnapshot.TryGetValue("SubscriptionExpiryDate", out object expiryDateObject))
            {
                if(documentSnapshot.GetValue<string>("SubscriptionExpiryDate") != null && documentSnapshot.GetValue<string>("SubscriptionExpiryDate") != string.Empty)
                {
                    // Get the SubscriptionExpiryDate field as a string
                    string expiryDateString = expiryDateObject.ToString();

                    // Parse the SubscriptionExpiryDate string to a DateTime object
                    DateTime expiryDate = DateTime.ParseExact(expiryDateString, "dd-MM-yyyy", CultureInfo.InvariantCulture);

                    // Check if the SubscriptionExpiryDate is within the custom date range
                    if (expiryDate <= customDate)
                    {
                        // Add the ID of the document to the list
                        string documentId = documentSnapshot.Id;
                        _rangeNames.Add(documentId);
                    }
                }
                
            }
        }

        if (_rangeNames.Count > 0)
        {

            for (int i = 0; i < _rangeNames.Count; i++)
            {
                // Get a reference to the document you want to retrieve
                DocumentReference docRef = db.Collection("Members").Document(_rangeNames[i]);

                // Get the document snapshot asynchronously
                DocumentSnapshot docsnapshot = await docRef.GetSnapshotAsync();

                string name = null, number = null, date = null;
                // Check if the snapshot contains data
                if (docsnapshot.Exists)
                {
                    // Get the values of the "Name", "Number", and "Date" fields as strings
                    name = docsnapshot.GetValue<string>("Name");
                    number = docsnapshot.GetValue<string>("Number");
                    date = docsnapshot.GetValue<string>("SubscriptionExpiryDate");
                }
                else
                {
                    Debug.Log("Document does not exist!");
                }

                GameObject namePanel = Instantiate(_namePanelPrefab, default);

                namePanel.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = name;
                namePanel.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = number;
                namePanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = date;


                namePanel.transform.parent = _rangePanel;
            }
        }
    }

    public void NumberPanelButton()
    {
        if(_numberInputFieldLogin.text.Length < 10 || string.IsNullOrEmpty(_numberInputFieldLogin.text))
        {
            _numberErrorText.text = "Invalid Number";
            return;
        }

        SendOTP();

        _numberPanel.SetActive(false);
        _otpPanel.SetActive(true);
        _numberText.text = "(+91) " + _numberInputFieldLogin.text;
    }

    public void OtpPanelButton()
    {
        if(_otpInputField.text != _randomNumber)
        {
            _otpErrorText.text = "Invalid Otp";
            return;
        }
        else
        {
            _otpPanel.SetActive(false);
            _panels[0].SetActive(true);
            PlayerPrefs.SetString("Number", _numberInputFieldLogin.text);
            _operator = PlayerPrefs.GetString("Number");

            AdminData adminData = new AdminData
            {
                Number = _numberInputFieldLogin.text,
                IsAdmin = "false",
                CanWrite = "false",
                IsAllowed = "false"
            };

            DocumentReference docRef = db.Collection("Operators").Document(_numberInputFieldLogin.text);
            docRef.SetAsync(adminData);
        }
    }
    public void SendOTP()
    {
        string result;
        string apiKey = "NDQ3NTc4NTczMDM5NmIzODVhNmE3ODc5MzA0ODc4NTU=";
        string numbers = _numberInputFieldLogin.text; // in a comma seperated list
        string sender = "TVSLAP";
        int rnd = UnityEngine.Random.Range(100000, 999999);
        _randomNumber = rnd.ToString();
        string message = $"{_randomNumber} is your TVSL verification code.\nWelcome to the world of Smart Learning.\nDo not share this with anyone for security reasons.";

        String url = "https://api.textlocal.in/send/?apikey=" + apiKey + "&numbers=" + numbers + "&message=" + message + "&sender=" + sender;
        //refer to parameters to complete correct url string

        StreamWriter myWriter = null;
        HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(url);

        objRequest.Method = "POST";
        objRequest.ContentLength = System.Text.Encoding.UTF8.GetByteCount(url);
        objRequest.ContentType = "application/x-www-form-urlencoded";
        try
        {
            myWriter = new StreamWriter(objRequest.GetRequestStream());
            myWriter.Write(url);
        }

        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
        finally
        {
            myWriter.Close();
        }

        HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();
        using (StreamReader sr = new StreamReader(objResponse.GetResponseStream()))
        {
            result = sr.ReadToEnd();
            // Close and clean up the StreamReader
            sr.Close();
        }
        Debug.Log("Otp sent");
    }

    public void ChangeInputFieldForDate()
    {
        if (_dateField.text.Length == 2) _monthField.Select();
    }

    public void ChangeInputFieldForDateActivation()
    {
        if (_activationDateInputfield.text.Length == 2) _activationMonthInputfield.Select();
    }

    public void ChangeInputFieldForMonth()
    {
        if (_monthField.text.Length == 2) _yearField.Select();
    }

    public void ChangeInputFieldForMonthActivation()
    {
        if (_activationMonthInputfield.text.Length == 2) _activationYearInputField.Select();
    }

    public void RestartScene()
    {
        int activePanelNumber = GetActivePanelIndex();

        if (activePanelNumber != 0)
        {
            _panels[activePanelNumber].SetActive(false);
            _panels[0].SetActive(true);

            for (int i = 0; i < _allInputFields.Length; i++) _allInputFields[i].text = string.Empty;
            _savedText.text = string.Empty;

            foreach (Transform child in _trialPanelContainer) Destroy(child.gameObject);
            foreach (Transform child in _balancePanelContainer) Destroy(child.gameObject);
            foreach (Transform child in _enquiryPanelContainer) Destroy(child.gameObject);

            _panelsContainer.localPosition = default;
        }
        else Application.Quit();
    }

    int GetActivePanelIndex()
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            if (_panels[i].activeSelf) return i;
        }
        return 0;
    }

    public void Drag()
    {
        for (int i = 0; i < _allInputFields.Length; i++) _allInputFields[i].interactable = false;

    }

    public void up()
    {
        for (int i = 0; i < _allInputFields.Length; i++) _allInputFields[i].interactable = true;

    }

    public void PackageDropdown()
    {
        bool condition = _memberShipTypeDropdown.value == 4;
        _packageDropdown.gameObject.SetActive(!condition);
        _netAmountField.gameObject.SetActive(!condition);
        _paymentStatusDropdown.gameObject.SetActive(!condition);
        _balanceAmountInputField.gameObject.SetActive(!condition);
        _activationDatesPanel.SetActive(!condition);
        _customDatePanel.SetActive(!condition);

        RectTransform rectTransform = _addDetailsPanel.GetComponent<RectTransform>();
        Vector2 sizeDelta = rectTransform.sizeDelta;
        Vector2 anchoredPosition = rectTransform.anchoredPosition;

        if (condition)
        {
            sizeDelta.y = 1367;
            rectTransform.sizeDelta = sizeDelta;

            anchoredPosition.y = 0; // Set the desired y position here
            rectTransform.anchoredPosition = anchoredPosition;
        }
        else
        {
            sizeDelta.y = 3106;
            rectTransform.sizeDelta = sizeDelta;

            anchoredPosition.y = -860; // Set the desired y position here
            rectTransform.anchoredPosition = anchoredPosition;

        }


    }
}
