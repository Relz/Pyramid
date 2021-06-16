using UnityEngine;
using Photon.Pun;
using System;

public class Piston : MonoBehaviour
{
    public GameObject MovingPart;
    public GameObject Indicator;
    public GameObject Arrow;
    public Material[] Materials;
    public Material BalancedIndicatorMaterial;
    public Material UnbalancedIndicatorMaterial;
    public int RowIndex;
    public int ColumnIndex;
    public int Weight
    {
        get => _weight;
        set => GetPhotonView().RPC("SetWeight", RpcTarget.All, (object)value);
    }
    public int Square
    {
        get => _square;
        set => GetPhotonView().RPC("SetSquare", RpcTarget.All, (object)value);
    }
    public float UnroundedLevel
    {
        get => _unroundedLevel;
        set => GetPhotonView().RPC("SetUnroundedLevel", RpcTarget.All, (object)value);
    }
    public int Level { get => Math.Min(MAX_LEVEL, Math.Max(0, (int)Math.Round(UnroundedLevel))); }
    public bool IsBalanced { get => PairedPiston != null && Math.Abs(Level - PairedPiston.GetComponent<Piston>().Level) <= 2; }
    public GameObject PairedPiston;
    public Action<Piston> OnMouseDownCallback;

    private PhotonView _photonView;
    private int _weight = 10;
    private int _square;
    private float _unroundedLevel;

    public const int MAX_LEVEL = 20;
    private const float MAX_ELEVATE = 0.4f;

    public void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    private PhotonView GetPhotonView()
    {
        return _photonView ?? GetComponent<PhotonView>();
    }

    public void Update()
    {
        Indicator.GetComponent<MeshRenderer>().material = IsBalanced ? BalancedIndicatorMaterial : UnbalancedIndicatorMaterial;
        MovingPart.transform.localPosition = new Vector3(
            MovingPart.transform.localPosition.x,
            ((float)Level / MAX_LEVEL * MAX_ELEVATE) - 0.5f * MAX_ELEVATE,
            MovingPart.transform.localPosition.z);
    }

    [PunRPC]
    public void SetMaterialIndex(int materialIndex)
    {
        Arrow.GetComponent<MeshRenderer>().material = Materials[materialIndex];
    }

    [PunRPC]
    public void SetPosition(Vector2 position)
    {
        RowIndex = (int)position.y;
        ColumnIndex = (int)position.x;
    }

    [PunRPC]
    public void SetWeight(int value)
    {
        _weight = value;
    }

    [PunRPC]
    public void SetSquare(int value)
    {
        _square = value;
    }

    [PunRPC]
    public void SetUnroundedLevel(float value)
    {
        _unroundedLevel = value;
        Piston pairedPiston = PairedPiston.GetComponent<Piston>();
        float pairedPistonUnroundedLevel = MAX_LEVEL - UnroundedLevel;
        if (pairedPiston.UnroundedLevel != pairedPistonUnroundedLevel)
        {
            pairedPiston.UnroundedLevel = pairedPistonUnroundedLevel;
        }
    }

    public void OnMouseDown()
    {
        if (GetPhotonView().Owner == PhotonNetwork.LocalPlayer)
        {
            OnMouseDownCallback(this);
        }
    }
}
