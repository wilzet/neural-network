using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    private readonly int[] numbers = new int[10];

    private void Awake()
    {
        if (!File.Exists(Application.persistentDataPath + "/Numbers"))
        {
            var numText = Resources.Load<TextAsset>("Numbers");
            File.WriteAllText(Application.persistentDataPath + "/Numbers", numText.text);
        }

        if (!File.Exists(Application.persistentDataPath + "/SavedNetwork"))
        {
            var saveText = Resources.Load<TextAsset>("SavedNetwork");
            File.WriteAllText(Application.persistentDataPath + "/SavedNetwork", saveText.text);
        }

        for (int i = 0; i < 10; i++)
        {
            if (!File.Exists(Application.persistentDataPath + "/" + i))
            {
                var dataText = Resources.Load<TextAsset>(i.ToString());
                File.WriteAllText(Application.persistentDataPath + "/" + i, dataText.text);
            }
        }
    }

    private void Start()
    {
        try
        {
            using StreamReader sr = new StreamReader(Application.persistentDataPath + "/Numbers");

            string[] line = sr.ReadLine().Trim().Split();

            for (int i = 0; i < 10; i++)
            {
                if (!int.TryParse(line[i], NumberStyles.Integer, new CultureInfo("en-US"), out numbers[i]))
                {
                    Debug.Log("Kunde ej \"parsea\" " + line[i]);
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogException(e);
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/Numbers");
            for (int i = 0; i < 10; i++)
            {
                sw.Write(numbers[i] + " ");
            }

            sw.WriteLine();
        }
        catch (IOException e)
        {
            Debug.LogException(e);
        }
    }

    public bool SaveDigit(double[] data)
    {
        int digit = (int) data[0];
        try
        {
            using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/" + digit, true);
            for (int i = 0; i < data.Length; i++)
            {
                sw.Write(data[i] + " ");
            }

            sw.WriteLine();
        }
        catch (IOException e)
        {
            Debug.Log("Kunde ej spara " + digit);
            Debug.LogException(e);
            return false;
        }

        numbers[digit]++;
        return true;
    }

    public bool GetDigit(int digit, out double[] data)
    {
        data = null;
        List<double> dataList = new List<double>();
        try
        {
            int digitIndex = Random.Range(0, numbers[digit]);
            using StreamReader sr = new StreamReader(Application.persistentDataPath + "/" + digit);

            for (int i = 0; i < digitIndex; i++)
            {
                sr.ReadLine();
            }

            string[] line = sr.ReadLine().Trim().Split();

            for (int i = 0; i < line.Length; i++)
            {
                if (!double.TryParse(line[i], NumberStyles.Float, new CultureInfo("en-US"), out double d))
                {
                    Debug.Log("Kunde inte sätta svaret");
                    return false;
                }

                dataList.Add(d);
            }
        }
        catch (IOException e)
        {
            Debug.Log("Kunde inte hämta " + digit);
            Debug.LogException(e);
            return false;
        }

        data = dataList.ToArray();
        return true;
    }
}