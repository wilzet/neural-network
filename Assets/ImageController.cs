using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DataManager))]
public class ImageController : MonoBehaviour
{
    [Range(1, 5)] public int brushSize = 2;
    public RawImage image;
    private Texture2D texture;
    private double[,] grid;
    private Vector2Int resolution;
    private DataManager data;
    private int ans = 0;

    private void Awake()
    {
        data = GetComponent<DataManager>();
    }

    public void Init(Vector2Int resolution)
    {
        if (this.resolution == resolution) return;

        this.resolution = resolution;
        grid = new double[resolution.x, resolution.y];

        texture = new Texture2D(resolution.x, resolution.y)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        float x = resolution.x % 2 == 0 ? 0f : .5f;
        float y = resolution.y % 2 == 0 ? 0f : .5f;
        image.texture = texture;
        image.rectTransform.sizeDelta = new Vector2(resolution.x, resolution.y);
        image.transform.position = new Vector2(x, y);
        Camera.main.transform.position = new Vector3(x, y, -10f);
        Camera.main.orthographicSize = resolution.y / 2f + 2f;

        Clear();
    }

    public void SaveDigit(int ans)
    {
        List<double> digitList = new List<double> { ans };
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                digitList.Add(grid[x, y]);
            }
        }

        if (!data.SaveDigit(digitList.ToArray()))
        {
            NeuralNetworkController.Log("Kunde ej spara siffran");
        }
    }

    public bool DisplayDigit(int digit)
    {
        if (!data.GetDigit(digit, out double[] digitArray)) return false;

        Clear();

        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                grid[x, y] = digitArray[y * resolution.x + x + 1];
                texture.SetPixel(x, y, GridColor(x, y));
            }
        }

        ans = (int) digitArray[0];
        texture.Apply();
        return true;
    }

    public double[] GetDigit(int digit)
    {
        if (!data.GetDigit(digit, out double[] digitArray))
        {
            return null;
        }

        return digitArray;
    }

    public double[] GetDigit()
    {
        List<double> dataList = new List<double> { ans };
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                dataList.Add(grid[x, y]);
            }
        }

        return dataList.ToArray();
    }

    public void Clear()
    {
        for (int y = 0; y < resolution.y; y++)
        {
            for (int x = 0; x < resolution.x; x++)
            {
                grid[x, y] = 0d;
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
    }

    public void Paint(Vector2 worldPos)
    {
        Vector2Int gridPos = Vector2Int.FloorToInt(worldPos) + resolution / 2;
        if (gridPos.x >= resolution.x || gridPos.x < 0 || gridPos.y >= resolution.y || gridPos.y < 0) return;

        if (brushSize > 1)
        {
            double radiusValue = brushSize * (brushSize - 1);
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (gridPos.y + y < 0 || gridPos.y + y >= resolution.y) continue;
                for (int x = -brushSize; x <= brushSize; x++)
                {
                    if (gridPos.x + x < 0 || gridPos.x + x >= resolution.x) continue;

                    double sqrDist = x * x + y * y;
                    if (sqrDist >= radiusValue) continue;

                    double a = (1d - (sqrDist / radiusValue)) * 15d * Time.deltaTime;
                    grid[gridPos.x + x, gridPos.y + y] = System.Math.Min(grid[gridPos.x + x, gridPos.y + y] + a, 1d);
                    texture.SetPixel(gridPos.x + x, gridPos.y + y, GridColor(gridPos.x + x, gridPos.y + y));
                }
            }
        }
        else
        {
            grid[gridPos.x, gridPos.y] = 1d;
            texture.SetPixel(gridPos.x, gridPos.y, Color.black);
        }

        texture.Apply();
    }

    private Color GridColor(int x, int y)
    {
        float cv = (float) (1d - grid[x, y]);
        return new Color(cv, cv, cv, 1f);
    }
}