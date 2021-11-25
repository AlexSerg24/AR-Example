using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using TMPro;

public class ObjectGenerator : MonoBehaviour
{
    public GameObject[] SpawnedObject; // array of objects
    public GameObject[] AddedObjects;
    public GameObject Particles;
    public GameObject ParticlesDestroy;
    public GameObject SpawnMarkerPrefab;

    private ARPlaneManager planeManager;
    private ARPlane firstPlane;

    private ARRaycastManager aRRaycastManager;
    private List<ARRaycastHit> hits;
    private GameObject SpawnMarker;
    [SerializeField] private GameObject SpawnObjectScroll;
    [SerializeField] private Camera ARCamera;
    [SerializeField] private GameObject SelectionPanel;
    [SerializeField] private TMP_Text SelectedTitle;
    [SerializeField] private TMP_Text SelectedDescription;
    private int SpawnObjectIndex = -1;
    private int[] SpawnObjectCount;
    private string[] SpawnObjectName;

    private int AddedObjectNumber = 0;
    private Vector3 nearPosition;
    private bool checkEnter = false;

    private enum ObjectGeneratorState { Inactive, ActiveSpawn, SpawnReady, Selection }
    private ObjectGeneratorState currentState;
    private GameObject selectedObject;

    private void Awake()
    {
        planeManager = GameObject.Find("AR Session Origin").GetComponent<ARPlaneManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // variable setup
        aRRaycastManager = FindObjectOfType<ARRaycastManager>();
        hits = new List<ARRaycastHit>();
        currentState = ObjectGeneratorState.Inactive;
        SpawnObjectCount = new int[SpawnedObject.Length];
        SpawnObjectName = new string[SpawnedObject.Length];
        for (int i = 0; i < SpawnedObject.Length; i++)
        {
            SpawnObjectCount[i] = 0;
            SpawnObjectName[i] = SpawnedObject[i].name;
        }

        // instantiate new spawn marker
        SpawnMarker = Instantiate(SpawnMarkerPrefab, new Vector3(0.0f, 0.0f, 0.0f), SpawnMarkerPrefab.transform.rotation);
        SpawnMarker.SetActive(false);

        // hide UI elements
        SpawnObjectScroll.SetActive(false);
        SelectionPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (firstPlane != null)
        {
            if (!checkEnter)
            {
                StartCoroutine(Wait(firstPlane));
                checkEnter = true;
            }
        }

        // process touch
        if (Input.touchCount > 0)
        {
            int touchCount = Input.touchCount;
            if (touchCount <= 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = touch.position;

            //process object movement and rotation
            if (currentState == ObjectGeneratorState.Selection)
            {
                if (touchCount == 1)
                {
                    MoveSelectedObject(touch);
                }
                else if (touchCount == 2)
                {
                    RotateSelectedObject(touch, Input.GetTouch(1));
                }
            }
            // process object selection
            if (currentState == ObjectGeneratorState.Inactive)
            {
                TrySelectObject(touchPosition);
            }

            // process object creation
            if (currentState == ObjectGeneratorState.ActiveSpawn || currentState == ObjectGeneratorState.SpawnReady)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    ShowMarker(true);
                    MoveMarker(touch.position);
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    MoveMarker(touch.position);
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (currentState == ObjectGeneratorState.SpawnReady)
                    {
                        SpawnObject();
                    }
                    else if (currentState == ObjectGeneratorState.ActiveSpawn)
                    {
                        currentState = ObjectGeneratorState.SpawnReady;
                    }
                    ShowMarker(false);
                }
            }
        }
    }

    private void OnEnable()
    {
        planeManager.planesChanged += CallbackPlanesChanged;
    }

    private void OnDisable()
    {
        planeManager.planesChanged -= CallbackPlanesChanged;
    }

    public void TrySelectObject(Vector2 pos)
    {
        Ray ray = ARCamera.ScreenPointToRay(pos);
        RaycastHit hitObject;
        if (Physics.Raycast(ray, out hitObject))
        {
            if (hitObject.collider.CompareTag("Print"))
            {
                selectedObject = hitObject.collider.gameObject.transform.parent.gameObject;
                SpawnedObject stats = selectedObject.GetComponent<SpawnedObject>();
                if (stats != null)
                {
                    currentState = ObjectGeneratorState.Selection;
                    //SelectionPanel.SetActive(true);
                    SelectedTitle.text = stats.Name;
                    SelectedDescription.text = stats.Description;
                    stats.ShouldDestroy = true;
                }
                //Destroy(hitObject.collider.gameObject.transform.parent.gameObject/*GetComponentInParent<Collider>().gameObject*/);
            }


            if (hitObject.collider.CompareTag("SpawnedObject"))
            {
            selectedObject = hitObject.collider.gameObject;
            SpawnedObject stats = selectedObject.GetComponent<SpawnedObject>();
                if (stats != null)
                {
                    currentState = ObjectGeneratorState.Selection;
                    SelectionPanel.SetActive(true);
                    SelectedTitle.text = stats.Name;
                    SelectedDescription.text = stats.Description;
                }
                else
                {
                    Debug.Log("it broke :(");
                }
            }
        }
    }

    void CallbackPlanesChanged(ARPlanesChangedEventArgs args)
    {
        List<ARPlane> planes = args.added;
        planes = args.removed;
        planes = args.updated;

        if (planes[0].size.x > 1 || planes[0].size.y > 1) 
        {
            firstPlane = planes[0];
        }
    }

    private void MoveSelectedObject(Touch touch)
    {
        if (touch.phase == TouchPhase.Moved)
        {
            aRRaycastManager.Raycast(touch.position, hits, TrackableType.Planes);
            selectedObject.transform.position = hits[0].pose.position;
        }
    }

    private void RotateSelectedObject(Touch touch, Touch touch2)
    {
        if (touch2.phase == TouchPhase.Moved || touch.phase == TouchPhase.Moved)
        {
            float distance = Vector2.Distance(touch2.position, touch.position);
            float distancePrev = Vector2.Distance(touch2.position - touch2.deltaPosition, touch.position - touch.deltaPosition);
            float delta = distance - distancePrev;

            if (Mathf.Abs(delta) > 0.0f) 
            {
                delta *= 0.1f; //affects the rotation speed
                selectedObject.transform.rotation *= Quaternion.Euler(0.0f, -touch.deltaPosition.x * delta, 0.0f);
            }
        }
    }

    void ShowMarker(bool value)
    {
        SpawnMarker.SetActive(value);
    }

    void MoveMarker(Vector2 touchPosition)
    {
        aRRaycastManager.Raycast(touchPosition, hits, TrackableType.Planes);
        SpawnMarker.transform.position = hits[0].pose.position;
    }

    void SpawnObject()
    {
        GameObject spawn = Instantiate(SpawnedObject[SpawnObjectIndex], SpawnMarker.transform.position, SpawnedObject[SpawnObjectIndex].transform.rotation);
        currentState = ObjectGeneratorState.Inactive;
        SpawnObjectScroll.SetActive(false);

        SpawnObjectCount[SpawnObjectIndex]++;
        SpawnedObject stats = spawn.GetComponent<SpawnedObject>();
        stats.Name = SpawnObjectName[SpawnObjectIndex] + " " + SpawnObjectCount[SpawnObjectIndex].ToString();
        stats.Description = "Описание объекта:\nИмя:" + stats.Name + "\nЦвет: " + spawn.GetComponent<Renderer>().material.color + "\nСоздатель: Игрок";
        stats.Object = spawn;
        stats.ParticlesDestroy = ParticlesDestroy;
    }

    public void ShowSelectionScroll()
    {
        SpawnObjectScroll.SetActive(true);
        CloseSelection();
    }

    public void ChooseTypeCube()
    {
        SpawnObjectIndex = 0;
        currentState = ObjectGeneratorState.ActiveSpawn;
    }

    public void ChooseTypeSphere()
    {
        SpawnObjectIndex = 1;
        currentState = ObjectGeneratorState.ActiveSpawn;
    }
    public void ChooseTypeCylinder()
    {
        SpawnObjectIndex = 2;
        currentState = ObjectGeneratorState.ActiveSpawn;
    }

    public void CloseSelection()
    {
        currentState = ObjectGeneratorState.Inactive;
        selectedObject = null;
        SelectionPanel.SetActive(false);
    }

    public void CloseSelectoinOnDoubleClick(int count)
    {
        if(currentState == ObjectGeneratorState.Selection)
        {
            if (count == 2)
            {
                CloseSelection();
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void LeanSwipeTest(float distance)
    {
        Debug.Log(distance);
    }

    private IEnumerator Wait(ARPlane Plane)
    {
        yield return new WaitForSeconds(1.0f);
        nearPosition = Plane.transform.position;
        StartCoroutine(AddParticles(nearPosition));
        AddedObjects[AddedObjectNumber].transform.rotation *= Quaternion.Euler(0.0f, AddedObjects[AddedObjectNumber].transform.rotation.y + Random.Range(0.0f, 360.0f), 0.0f);
        GameObject spawn1 = Instantiate(AddedObjects[AddedObjectNumber], nearPosition, AddedObjects[AddedObjectNumber].transform.rotation);
        SpawnedObject stats1 = spawn1.GetComponent<SpawnedObject>();
        stats1.Name = spawn1.name;
        stats1.Name = stats1.Name.Remove(8);
        stats1.Description = "Описание объекта:\nИмя:" + stats1.Name + "\nЦвет: " + spawn1.GetComponent<Renderer>().material.color + "\nСоздатель: Автоматически сгенерирован";
        stats1.Object = spawn1;
        stats1.ParticlesDestroy = ParticlesDestroy;
        AddedObjectNumber++;

        yield return new WaitForSeconds(2.0f);
        nearPosition = new Vector3(Plane.transform.position.x - Random.Range(0.3f,0.6f), Plane.transform.position.y, Plane.transform.position.z - Random.Range(0.3f, 0.6f));
        StartCoroutine(AddParticles(nearPosition));
        AddedObjects[AddedObjectNumber].transform.rotation *= Quaternion.Euler(0.0f, AddedObjects[AddedObjectNumber].transform.rotation.y + Random.Range(0.0f, 360.0f), 0.0f);
        GameObject spawn2 = Instantiate(AddedObjects[AddedObjectNumber], nearPosition, AddedObjects[AddedObjectNumber].transform.rotation);
        SpawnedObject stats2 = spawn2.GetComponent<SpawnedObject>();
        stats2.Name = spawn2.name;
        stats2.Name = stats2.Name.Remove(8);
        stats2.Description = "Описание объекта:\nИмя:" + stats2.Name + "\nЦвет: " + spawn2.GetComponent<Renderer>().material.color + "\nСоздатель: Автоматически сгенерирован";
        stats2.Object = spawn2;
        stats2.ParticlesDestroy = ParticlesDestroy;
        AddedObjectNumber++;

        yield return new WaitForSeconds(2.0f);
        nearPosition = new Vector3(Plane.transform.position.x + Random.Range(0.3f, 0.6f), Plane.transform.position.y, Plane.transform.position.z + Random.Range(0.3f, 0.6f));
        StartCoroutine(AddParticles(nearPosition));
        AddedObjects[AddedObjectNumber].transform.rotation *= Quaternion.Euler(0.0f, AddedObjects[AddedObjectNumber].transform.rotation.y + Random.Range(0.0f, 360.0f), 0.0f);
        GameObject spawn3 = Instantiate(AddedObjects[AddedObjectNumber], nearPosition, AddedObjects[AddedObjectNumber].transform.rotation);
        SpawnedObject stats3 = spawn3.GetComponent<SpawnedObject>();
        stats3.Name = spawn3.name;
        stats3.Name = stats3.Name.Remove(8);
        stats3.Description = "Описание объекта:\nИмя:" + stats3.Name + "\nЦвет: " + spawn3.GetComponent<Renderer>().material.color + "\nСоздатель: Автоматически сгенерирован";
        stats3.Object = spawn3;
        stats3.ParticlesDestroy = ParticlesDestroy;
        AddedObjectNumber++;

        yield return new WaitForSeconds(2.0f);
        nearPosition = new Vector3(Plane.transform.position.x - Random.Range(0.3f, 0.6f), Plane.transform.position.y, Plane.transform.position.z + Random.Range(0.3f, 0.6f));
        StartCoroutine(AddParticles(nearPosition));
        AddedObjects[AddedObjectNumber].transform.rotation *= Quaternion.Euler(0.0f, AddedObjects[AddedObjectNumber].transform.rotation.y + Random.Range(0.0f, 360.0f), 0.0f);
        GameObject spawn4 = Instantiate(AddedObjects[AddedObjectNumber], nearPosition, AddedObjects[AddedObjectNumber].transform.rotation);
        SpawnedObject stats4 = spawn4.GetComponent<SpawnedObject>();
        stats4.Name = spawn4.name;
        stats4.Name = stats4.Name.Remove(8);
        stats4.Description = "Описание объекта:\nИмя:" + stats4.Name + "\nЦвет: " + spawn4.GetComponent<Renderer>().material.color + "\nСоздатель: Автоматически сгенерирован";
        stats4.Object = spawn4;
        stats4.ParticlesDestroy = ParticlesDestroy;
        AddedObjectNumber++;

        yield return new WaitForSeconds(2.0f);
        nearPosition = new Vector3(Plane.transform.position.x + Random.Range(0.3f, 0.6f), Plane.transform.position.y, Plane.transform.position.z - Random.Range(0.3f, 0.6f));
        StartCoroutine(AddParticles(nearPosition));
        AddedObjects[AddedObjectNumber].transform.rotation *= Quaternion.Euler(0.0f, AddedObjects[AddedObjectNumber].transform.rotation.y + Random.Range(0.0f, 360.0f), 0.0f);
        GameObject spawn5 = Instantiate(AddedObjects[AddedObjectNumber], nearPosition, AddedObjects[AddedObjectNumber].transform.rotation);
        SpawnedObject stats5 = spawn5.GetComponent<SpawnedObject>();
        stats5.Name = spawn5.name;
        stats5.Name = stats5.Name.Remove(8);
        stats5.Description = "Описание объекта:\nИмя:" + stats5.Name + "\nЦвет: " + spawn5.GetComponent<Renderer>().material.color + "\nСоздатель: Автоматически сгенерирован";
        stats5.Object = spawn5;
        stats5.ParticlesDestroy = ParticlesDestroy;
        AddedObjectNumber++;
    }

    private IEnumerator AddParticles(Vector3 pos)
    {
        GameObject particles = Instantiate(Particles,pos, Particles.transform.rotation);
        yield return new WaitForSeconds(1.5f);
        Destroy(particles);
    }
}
