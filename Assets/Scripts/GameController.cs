using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustomProperty
{
    public static string TargetFound = "TargetFound";
}

public class GameController : MonoBehaviourPunCallbacks
{
    public GameObject ImageTarget;
    public GameObject Timer;
    public GameObject Hint;
    public GameObject WeightsPanel;
    public GameObject GameOverWindow;
    public Text GameOverInfoMessage;
    public GameObject[] Pistons;

    private PhotonView _photonView;
    private GameObject[,] _pistonsMatrix = new GameObject[4, 4];
    private Text _timerValue;
    private int _weight = 0;
    private float _secondsPassed = 0;
    private int _integerSecondsPassed = 0;
    private bool _gameOver = false;

    private const int _goodTimeLimit = 60;
    private const int _normalTimeLimit = 90;

    private readonly Color _timerGoodColor = new Color32(0, 149, 39, 255);
    private readonly Color _timerNormalColor = new Color32(174, 125, 0, 255);
    private readonly Color _timerBadColor = new Color32(220, 39, 41, 255);


    public void Awake()
    {
        Application.targetFrameRate = 30;
        _photonView = GetComponent<PhotonView>();
        _timerValue = Timer.GetComponentInChildren<Text>();
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        if (PhotonNetwork.IsMasterClient)
        {
            InitializePistonsMatrix();
        }
        SubscribeToPistonsMouseDown();
    }

    public void Update()
    {
        if (IsGameStarted() && !_gameOver)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateSecondsPassed();
                CheckWinCondition();
            }
        }
        if (_gameOver)
        {
            WeightsPanel.SetActive(false);
            GameOverWindow.SetActive(true);
        }
    }

    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void SetWeight(int value)
    {
        _weight = value;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (!_gameOver)
        {
            _gameOver = true;
            GameOverInfoMessage.text = "2-ой игрок вышел из игры =(";
        }
    }

    public void OnTargetFound()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, true);
        Timer.SetActive(true);
        WeightsPanel.SetActive(true);
        Hint.SetActive(false);
    }

    public void OnTargetLost()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        Timer.SetActive(false);
        WeightsPanel.SetActive(false);
        Hint.SetActive(true);
    }

    [PunRPC]
    public void SetSecondsPassed(int secondsPassed)
    {
        _integerSecondsPassed = secondsPassed;
        _timerValue.text = FormatSecondsPassed(secondsPassed);
        UpdateTimerColor(secondsPassed);
    }

    [PunRPC]
    public void Win()
    {
        _gameOver = true;
        GameOverInfoMessage.text = $"{GetCongratulationMessage()}\n\nВы прошли игру за {FormatSecondsPassed(_integerSecondsPassed)}";
    }

    [PunRPC]
    public void SetPairedPistons(Vector2 pairedPistonsIndex)
    {
        GameObject firstPistonGameObject = Pistons[(int)pairedPistonsIndex.x];
        GameObject secondPistonGameObject = Pistons[(int)pairedPistonsIndex.y];
        firstPistonGameObject.GetComponent<Piston>().PairedPiston = secondPistonGameObject;
        secondPistonGameObject.GetComponent<Piston>().PairedPiston = firstPistonGameObject;
    }

    private void UpdateTimerColor(int secondsPassed)
    {
        _timerValue.color = secondsPassed < _goodTimeLimit
            ? _timerGoodColor
            : secondsPassed < _normalTimeLimit
                ? _timerNormalColor
                : _timerBadColor;
    }

    private void SetPlayerTargetFound(Photon.Realtime.Player player, bool value)
    {
        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable.Add(PlayerCustomProperty.TargetFound, value);
        player.SetCustomProperties(hashtable);
    }

    private void InitializePistonsMatrix()
    {
        FillPistonsMatrix();
        PairPistons();
        GeneratePistonsLevel();
        GeneratePistonsSquare();
        SetPistonsOwner();
    }

    private void FillPistonsMatrix()
    {
        int rowCount = _pistonsMatrix.GetLength(0);
        int columnCount = _pistonsMatrix.GetLength(1);

        for (int rowIndex = 0; rowIndex < rowCount; ++rowIndex)
        {
            for (int columnIndex = 0; columnIndex < columnCount; ++columnIndex)
            {
                GameObject pistonGameObject = Pistons[rowIndex * rowCount + columnIndex];
                _pistonsMatrix[rowIndex, columnIndex] = pistonGameObject;
                PhotonView pistonPhotonView = pistonGameObject.GetComponent<PhotonView>();
                Vector2 pistonPosition = new Vector2(columnIndex, rowIndex);
                pistonPhotonView.RPC("SetPosition", RpcTarget.All, (object)pistonPosition);
            }
        }
    }

    private void PairPistons()
    {
        List<GameObject> pistonsToPair = Pistons.ToList();

        while (pistonsToPair.Count != 0)
        {
            GameObject firstPistonGameObject = pistonsToPair.First();
            int secondPistonIndex = UnityEngine.Random.Range(1, pistonsToPair.Count);
            GameObject secondPistonGameObject = pistonsToPair[secondPistonIndex];
            Vector2 pistonsIndex = new Vector2(Array.IndexOf(Pistons, firstPistonGameObject), Array.IndexOf(Pistons, secondPistonGameObject));
            _photonView.RPC("SetPairedPistons", RpcTarget.All, (object)pistonsIndex);
            pistonsToPair.RemoveAt(secondPistonIndex);
            pistonsToPair.RemoveAt(0);
        }
    }

    private void GeneratePistonsLevel()
    {
        ISet<GameObject> pistonsToSetUnroundedLevel = new HashSet<GameObject>(Pistons);

        while (pistonsToSetUnroundedLevel.Count != 0)
        {
            GameObject pistonGameObject = pistonsToSetUnroundedLevel.First();
            Piston piston = pistonGameObject.GetComponent<Piston>();
            GameObject pairedPistonGameObject = piston.PairedPiston;
            piston.UnroundedLevel = UnityEngine.Random.Range(0, 20);
            pistonsToSetUnroundedLevel.Remove(pistonGameObject);
            pistonsToSetUnroundedLevel.Remove(pairedPistonGameObject);
        }
    }

    private void GeneratePistonsSquare()
    {
        foreach (GameObject pistonGameObject in Pistons)
        {
            Piston piston = pistonGameObject.GetComponent<Piston>();
            piston.Square = UnityEngine.Random.Range(1000, 5000);
        }
    }

    private void SetPistonsOwner()
    {
        ISet<GameObject> pistonsToSetOwner = new HashSet<GameObject>(Pistons);

        while (pistonsToSetOwner.Count != 0)
        {
            GameObject firstPistonGameObject = pistonsToSetOwner.First();
            Piston firstPiston = firstPistonGameObject.GetComponent<Piston>();
            PhotonView firstPistonPhotonView = firstPistonGameObject.GetComponent<PhotonView>();

            GameObject secondPistonGameObject = firstPiston.PairedPiston;
            PhotonView secondPistonPhotonView = secondPistonGameObject.GetComponent<PhotonView>();

            Photon.Realtime.Player secondPlayer = PhotonNetwork.PlayerListOthers.First();

            bool isFirstPistonOwnedByFirstPlayer = UnityEngine.Random.value > 0.5f;

            Photon.Realtime.Player firstPistonOwner = isFirstPistonOwnedByFirstPlayer ? PhotonNetwork.MasterClient : secondPlayer;
            int firstPistonMaterialIndex = isFirstPistonOwnedByFirstPlayer ? 0 : 1;

            Photon.Realtime.Player secondPistonOwner = isFirstPistonOwnedByFirstPlayer ? secondPlayer : PhotonNetwork.MasterClient;
            int secondPistonMaterialIndex = isFirstPistonOwnedByFirstPlayer ? 1 : 0;

            firstPistonPhotonView.TransferOwnership(firstPistonOwner);
            firstPistonPhotonView.RPC("SetMaterialIndex", RpcTarget.All, (object)firstPistonMaterialIndex);

            secondPistonPhotonView.TransferOwnership(secondPistonOwner);
            secondPistonPhotonView.RPC("SetMaterialIndex", RpcTarget.All, (object)secondPistonMaterialIndex);

            pistonsToSetOwner.Remove(firstPistonGameObject);
            pistonsToSetOwner.Remove(secondPistonGameObject);
        }
    }

    private void SubscribeToPistonsMouseDown()
    {
        foreach (GameObject pistonGameObject in Pistons)
        {
            Piston piston = pistonGameObject.GetComponent<Piston>();
            piston.OnMouseDownCallback = OnPistonMouseDown;
        }
    }

    private void OnPistonMouseDown(Piston piston)
    {
        piston.Weight += _weight;
        GameObject pairedPistonGameObject = piston.PairedPiston;
        Piston pairedPiston = pairedPistonGameObject.GetComponent<Piston>();

        float dH = piston.Weight / (float)piston.Square - pairedPiston.Weight / (float)pairedPiston.Square;
        float upperBound = piston.Weight / (float)piston.Square + pairedPiston.Weight / (float)pairedPiston.Square;
        float unroundedLevelDifference = dH / upperBound * Piston.MAX_LEVEL;
        float halfPistonLevel = Piston.MAX_LEVEL * 0.5f;

        float unroundedLevel = halfPistonLevel - unroundedLevelDifference * 0.5f;
        piston.UnroundedLevel = unroundedLevel;
    }

    private bool IsGameStarted()
    {
        return PhotonNetwork.PlayerList.All(GetPlayerTargetFound);
    }

    private bool GetPlayerTargetFound(Photon.Realtime.Player player)
    {
        return player.CustomProperties.ContainsKey(PlayerCustomProperty.TargetFound)
            && (bool)player.CustomProperties[PlayerCustomProperty.TargetFound];
    }

    private void CheckWinCondition()
    {
        bool AreAllPistonsBalanced = Pistons.All((GameObject pistonGameObject) => pistonGameObject.GetComponent<Piston>().IsBalanced);
        if (AreAllPistonsBalanced)
        {
            _photonView.RPC("Win", RpcTarget.All);
        }
    }

    private string GetCongratulationMessage()
    {
        return _integerSecondsPassed < _goodTimeLimit
            ? "Отлично!"
            : _integerSecondsPassed < _normalTimeLimit
                ? "Хорошо!"
                : "Неплохо!";
    }

    private string FormatSecondsPassed(int secondsPassed)
    {
        return string.Format("{0:D2}:{1:D2}", secondsPassed / 60, secondsPassed % 60);
    }

    private void UpdateSecondsPassed()
    {
        _secondsPassed += Time.deltaTime;
        int newIntegerSecondsPassed = (int)Math.Floor(_secondsPassed);
        if (newIntegerSecondsPassed > _integerSecondsPassed)
        {
            _integerSecondsPassed = newIntegerSecondsPassed;
            _photonView.RPC("SetSecondsPassed", RpcTarget.All, (object)newIntegerSecondsPassed);
        }
    }
}
