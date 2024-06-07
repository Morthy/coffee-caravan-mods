
using Il2CppBroccoliGames;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Twitch;


public class TwitchMod : MelonMod
{
    private const int MinimumNames = 4; // Minimum names before visitors start using names
    
    private readonly Dictionary<int, string> _customerNames = new();
    private int _nameCounter;
    private string? _nameFilePath;
    private DateTime? _lastNamesUpdate;
    private string[]? _names;

    public override void OnInitializeMelon()
    {
        _nameFilePath = Path.Combine(Path.GetDirectoryName(MelonAssembly.Location)!,  "TwitchNames.txt");
        LoggerInstance.Msg($"Customer names will be loaded from {_nameFilePath}");
    }
    
    private void UpdateNames()
    {
        LoggerInstance.Msg("Updating names");
        _lastNamesUpdate = DateTime.Now;

        if (!File.Exists(_nameFilePath))
        {
            LoggerInstance.Msg("Names file does not exist");
            _names = null;
            return;
        }
        
        _names = File.ReadAllLines(_nameFilePath);
        LoggerInstance.Msg($"Updated names files with {_names.Length} names");
    }

    private string[]? GetPotentialNames()
    {
        if (_lastNamesUpdate == null || (DateTime.Now - (DateTime)_lastNamesUpdate).TotalSeconds > 1)
        {
            UpdateNames();
        }

        return _names;
    }

    private string? GetNextName()
    {
        var names = GetPotentialNames();
        
        if (names == null || names.Length < MinimumNames)
        {
            return null;
        }

        if (names.Length < _nameCounter + 1)
        {
            _nameCounter = 0;
        }
        
        var name= names[_nameCounter];
        _nameCounter++;
        return name;
    }

    public override void OnLateUpdate()
    {
        var customers = Object.FindObjectsOfType<CustomerItemHandler>();
        
        foreach (var customer in customers)
        {
            // Try and determine a name for a customer without a name
            if (!_customerNames.ContainsKey(customer.gameObject.GetInstanceID()))
            {
                var name = GetNextName();

                if (name == null)
                {
                    continue;
                }
                
                LoggerInstance.Msg($"Enable customer with name {name}");
                _customerNames[customer.gameObject.GetInstanceID()] = name;
                customer.gameObject.GetComponent<CustomerUIDisplay>().orderName.autoSizeTextContainer = true;
            }
            
            var canvas = customer.gameObject.transform.Find("Canvas");
            canvas.GetComponent<Canvas>().enabled = true; // Force enable the canvas, because the game disables it if the customer isn't in the queue or waiting for their order

            // Because we enable the canvas, we need to manually hide the order images/patience display in situations where it shouldn't be seen 
            var hideStuff = (customer.customerLifecycle.currentCustomerStep == CustomerLifecycle.CustomerStep.SittingAtTable && customer.customerLifecycle.stepDestinationReached == false)
                            || customer.customerLifecycle.currentCustomerStep == CustomerLifecycle.CustomerStep.ProcessingOrder || customer.customerLifecycle.currentCustomerStep == CustomerLifecycle.CustomerStep.Leaving;


            canvas.transform.Find("PatienceOuterCircle").gameObject.transform.localScale = hideStuff ? Vector3.zero : Vector3.one;
            canvas.transform.Find("PatienceInnerCircle").gameObject.transform.localScale = hideStuff ? Vector3.zero : Vector3.one;
            canvas.transform.Find("PatienceRing").gameObject.transform.localScale = hideStuff ? Vector3.zero : Vector3.one;
            canvas.transform.Find("OrderImage").gameObject.transform.localScale = hideStuff ? Vector3.zero : Vector3.one;
            
            // Adjust the text position depending on if the patience UI is shown and set the text content
            customer.gameObject.GetComponent<CustomerUIDisplay>().orderName.gameObject.transform.localPosition = hideStuff ? new Vector3(0f, 0.1f, 0f) : new Vector3(0f, 0.4f, 0f);
            customer.gameObject.GetComponent<CustomerUIDisplay>().orderName.gameObject.active = true;
            customer.gameObject.GetComponent<CustomerUIDisplay>().orderName.text = _customerNames[customer.gameObject.GetInstanceID()];
        }
    }
}