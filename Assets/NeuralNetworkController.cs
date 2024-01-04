using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NeuralNetworkController : MonoBehaviour
{
    private NeuralNetwork nn;
    public Vector2Int resolution = new Vector2Int(25, 25);
    public int[] layers = new int[] { 1, 16, 16, 10 };
    [Range(1, 1000)] public int batchSize = 100;
    [Min(1)] public int epochs = 100;
    [Range(-1, 5)] public double learnRate = 1d;
    [Range(0, .9999f)] public float accuracy = .76f;
    public ImageController image = default;
    public TMP_InputField saveButtonInputField = default;
    public Slider learnRateSlider = default;
    public Slider accuracySlider = default;
    public GameObject help = default;
    private static TMP_Text log;
    private static int lines;
    private bool training;

    private void Awake()
    {
        if (image == null)
        {
            Debug.Log("Ingen image controller");
            Debug.Break();
        }

        if (log == null)
        {
            foreach (var l in FindObjectsOfType<TMP_Text>())
            {
                if (string.Equals(l.name, "Log"))
                {
                    log = l;
                    log.text = "";
                    break;
                }
            }
        }
    }

    private void Start()
    {
        Init();
        lines = 0;

        UpdateSliders();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            help.SetActive(!help.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            nn.Feed(image.GetDigit());
            Log(nn.GetOutput());
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (!training)
            {
                StartCoroutine(Train());
            }
            else
            {
                Log("Pågående träning");
            }
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!training)
            {
                StartCoroutine(Train(accuracy));
            }
            else
            {
                Log("Pågående träning");
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            var saveText = Resources.Load<TextAsset>("SavedNetwork");
            File.WriteAllText(Application.persistentDataPath + "/SavedNetwork", saveText.text);

            LoadNetwork();
        }

        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    PrintNetwork(true);
        //}

        if (Input.GetKey(KeyCode.A))
        {
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown((KeyCode) (48 + i)))   //Alpha0 ... Alpha9
                {
                    if (image.DisplayDigit(i))
                    {
                        Log("Laddade in " + i);
                        nn.Feed(image.GetDigit());
                        Log("Kostnad: " + nn.Cost());
                    }

                    break;
                }
            }
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            image.Paint(pos);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            image.Clear();
        }
    }

    private void Init()
    {
        layers[0] = resolution.x * resolution.y;
        nn = new NeuralNetwork(layers, batchSize, learnRate);

        image.Init(resolution);
    }

    public void UpdateSliders()
    {
        var v = learnRateSlider.value;
        learnRate = v < .95d || v > 1.05d ? v : 1d;
        learnRateSlider.value = (float)learnRate;
        accuracy = accuracySlider.value;

        learnRateSlider.GetComponentInChildren<TMP_Text>().text = "Inlärningstakt: " + learnRate.ToString("0.##");
        accuracySlider.GetComponentInChildren<TMP_Text>().text = "Precision: " + (accuracy * 100).ToString("0.##") + "%";
    }

    public void SaveImage()
    {
        if (!int.TryParse(saveButtonInputField.text, out int ans))
        {
            Log("Kan inte spara bilden, texten \"" + saveButtonInputField.text + "\" är ej giltig");
            return;
        }

        if (ans >= 0 && ans < 10)
        {
            image.SaveDigit(ans);
            Log("Sparade " + ans);
        }
        else
        {
            Log("Kan endast spara siffror i omfånget 0-9");
        }
    }

    public void SaveNetwork()
    {
        if (training) return;

        Log("Sparar Neuronnät...");
        if (nn.SaveNetwork())
        {
            Log("Neuronnät sparat");
        }
        else
        {
            Log("Sparning av Neuronnät misslyckades");
        }
    }

    public void LoadNetwork()
    {
        if (training) return;

        Log("Laddar in neuronnät...");
        if (nn.LoadNetwork())
        {
            Log("Neuronnät inladdat");
            if (resolution.x * resolution.y != layers[0])
            {
                Log("Upplösningen matchar ej input-lagret");
            }
        }
        else
        {
            Log("Inladdning av Neuronnätet misslyckades");
        }
    }

    public void NewNetwork()
    {
        Init();
        Log("Skapade ett nytt Neuronnät");
    }

    private void PrintNetwork(bool skipInput = false, bool printCost = true)
    {
        for (int i = skipInput ? 1 : 0; i < layers.Length; i++)
        {
            for (int j = 0; j < layers[i]; j++)
            {
                Log(nn.NeuronInfo(i, j));
            }
        }

        if (printCost)
        {
            Log("Kostnad: " + nn.Cost());
        }
    }

    private IEnumerator Train()
    {
        training = true;
        Log("Tränar på " + batchSize * epochs + " exempel...");
        int count = 0;
        var t = System.DateTime.UtcNow;

        for (int i = 0; i < epochs; i++)
        {
            for (int j = 0; j < batchSize; j++)
            {
                nn.Train(image.GetDigit(count));
                if (++count >= 10) count = 0;
            }

            yield return null;
        }

        training = false;
        Log("Träning avklarad på " + (System.DateTime.UtcNow - t).Seconds.ToString() + "s");
    }

    private IEnumerator Train(float a)
    {
        training = true;
        Log("Tränar för att få " + (a * 100f).ToString("0.##") + "% noggrannhet...");
        int count = 0;
        int totalCounter = 0;
        var t = System.DateTime.UtcNow;

        while (nn.Accuracy() < a)
        {
            for (int j = 0; j < batchSize; j++)
            {
                nn.Train(image.GetDigit(count));
                if (++count >= 10) count = 0;
            }

            totalCounter += batchSize;
            yield return null;
        }

        training = false;
        Log("Träning på " + totalCounter + " exempel avklarad på " + (System.DateTime.UtcNow - t).Seconds.ToString() + "s");
    }

    public static void Log(string text)
    {
        lines += text.Split('\n').Length;
        text += "\n";
        log.text += text;
        if (lines > 35)
        {
            string[] s = log.text.Split('\n');
            List<string> sList = new List<string>();
            for (int i = lines - 35; i < lines; i++)
            {
                sList.Add(s[i]);
            }

            log.text = string.Join("\n", sList.ToArray());
            log.text += "\n";
            lines = 35;
        }
    }
}