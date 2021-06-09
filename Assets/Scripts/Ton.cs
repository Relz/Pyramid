using UnityEngine;
using UnityEngine.UI;

public class Ton : MonoBehaviour
{
    public Ton[] AnotherTons;
    public Sprite OnSprite;
    public Sprite OffSprite;
    public bool IsDefault = false;

    private Image _image;
    private bool _selected;

    public void Awake()
    {
        _image = GetComponent<Image>();
        if (IsDefault)
        {
            GetComponent<Button>().onClick.Invoke();
        }
    }

    public void OnClick()
    {
        Selected = true;
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (Selected != value)
            {
                _selected = value;
                _image.sprite = _selected ? OnSprite : OffSprite;
                if (_selected)
                {
                    SwitchOffAnotherTons();
                }
            }
        }
    }

    private void SwitchOffAnotherTons()
    {
        foreach (Ton anotherToggle in AnotherTons)
        {
            anotherToggle.Selected = false;
        }
    }
}
