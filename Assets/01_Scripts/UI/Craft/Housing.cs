using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Housing : MonoBehaviour
{
    public GameObject itemPrefab;
    public GameObject previewGreen;
    public GameObject previewRed;
    public Transform player;

    public RaycastHit hit;
    public LayerMask layerMask;
    public float rayRange;
    public Material green;

    bool createdPreview = false;
    bool isBuildable = true;

    void Start()
    {

    }

    void Update()
    {
        CreatePreview();

        if (createdPreview)
        {
            UpdatePreviewLocation();
        }

        SetObjectLocation();
    }

    void CreatePreview()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            //itemPrefab = Instantiate(itemData, ī�޶� ������, Quaternion.identity);
            previewGreen = Instantiate(previewGreen, player.position + player.forward * 2f, Quaternion.identity, this.gameObject.transform);

            createdPreview = true;
        }
    }

    void UpdatePreviewLocation()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

        Debug.DrawRay(ray.origin, ray.direction * 20, Color.red, 0.1f);
        if(Physics.Raycast(ray, out hit, rayRange, layerMask))
        {
            if(hit.transform != null)
            {
                Vector3 location = hit.point;
                previewGreen.gameObject.transform.position = location;
            }
        }
    }

    void SetObjectLocation()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if(isBuildable)
            {
            Instantiate(itemPrefab, hit.point, Quaternion.identity, this.gameObject.transform);
            previewGreen.gameObject.SetActive(false); // ���߿��� ������ ������ �޾ƿ��ϱ� ��Ʈ���� �� null �ʱ�ȭ�ϱ�
            createdPreview = false;
            }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("HousingObject"))
        {
            isBuildable = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("HousingObject"))
        {
            isBuildable = true;;
        }
    }

}
