using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

public class DeliveryOrderSystem : MonoBehaviour
{

    [Header("�ֹ� ����")]
    public float ordergeneratelnterval = 15f;                               // �ֹ� ���� ����
    public int maxActiveOrders = 8;                                 // �ִ� �ֹ� ����

    [Header("���� ����")]
    public int totalOrdersCompleted = 0;
    public int completedOrders = 0;
    public int expiredOrders = 0;

    //�ֹ� ����Ʈ
    private List<DeliveryOrder> currentOrders = new List<DeliveryOrder>();

    //Builing ����
    private List<Building> restaurants = new List<Building>();
    private List<Building> customers = new List<Building>();

    //Event �ý���
    [System.Serializable]
    public class OrderSystemEvents
    {
        public UnityEvent<DeliveryOrder> OnNewOrderAdded;
        public UnityEvent<DeliveryOrder> OnOrderPickedUp;
        public UnityEvent<DeliveryOrder> OnOrderCompleted;
        public UnityEvent<DeliveryOrder> OnOrderExpired;
    }

    public OrderSystemEvents orderEvents;
    private DeliveryDriver driver;
    // Start is called before the first frame update
    void Start()
    {
        driver = FindObjectOfType<DeliveryDriver>(); 
        FindAIIBuilding();                                                  //�ǹ� �ʱ� ����

        //�ʱ� �ֹ� ����
        StartCoroutine(GeneratelnitialOrders());
        //�ֱ��� �ֹ� ����
        StartCoroutine(orderGenerator());
        //�ֱ��� ���� �ֹ� üũ
        StartCoroutine(ExpiredOrderChecker());
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FindAIIBuilding()
    {
        Building[] allBuildings = FindObjectsOfType<Building>();

        foreach (Building building in allBuildings)
        {
            if (building.BuildingType == BuildingType.Restaurant)
            {
                restaurants.Add(building);
            }
            else if (building.BuildingType == BuildingType.Customer)
            {
                customers.Add(building);
            }
        }

        Debug.Log($"������{restaurants.Count}��, ��{customers.Count} �� �߰�");
    }

    void CreataNewOrder()
    {
        if (restaurants.Count == 0 || customers.Count == 0) return;

        //���� �������� �� ����
        Building randomResturant = restaurants[Random.Range(0, restaurants.Count)];
        Building randomcustomer = customers[Random.Range(0, customers.Count)];

        //���� �ǹ��̸� �ٽ� ���� 
        if (randomResturant == randomcustomer)
        {
            randomcustomer = customers[Random.Range(0, customers.Count)];
        }

        float reward = Random.Range(3000, 8000);

        DeliveryOrder newOrder = new DeliveryOrder(++totalOrdersCompleted, randomResturant, randomcustomer, reward);

        currentOrders.Add(newOrder);
        orderEvents.OnNewOrderAdded?.Invoke(newOrder);
    }

    void PickupOrder(DeliveryOrder order)                       //�Ⱦ� �Լ�
    {
        order.state = OrderState.PickedUP;
        orderEvents.OnOrderPickedUp?.Invoke(order);
    }

    void CompleteOrder(DeliveryOrder order)                     //��� �Ϸ� �Լ�
    {
        order.state = OrderState.Completed;
        completedOrders++;

        //���� ����
        if (driver != null)
        {
            driver.AddMoney(order.reward);
        }

        //�Ϸ�� �ֹ� ����
        currentOrders.Remove(order);
        orderEvents.OnOrderCompleted?.Invoke(order);
    }

    void ExpireOrder(DeliveryOrder order)                       //�ֹ� ��� �Ҹ�
    {
        order.state = OrderState.Expired;
        expiredOrders++;

        currentOrders.Remove(order);
        orderEvents.OnOrderExpired?.Invoke(order);
    }

    //UI ���� ����
    public List<DeliveryOrder> GetCurrentOrders()
    {
        return new List<DeliveryOrder>(currentOrders);
    }

    public int GetPickWaitingCount()
    {
        int count = 0;
        foreach (DeliveryOrder order in currentOrders)
        {
            if (order.state == OrderState.WaitingPickup) count++;
        }
        return count;
    }
    public int GetDeliveryWaitingCount()
    {
        int count = 0;
        foreach (DeliveryOrder order in currentOrders)
        {
            if (order.state == OrderState.PickedUP) count++;
        }
        return count;
    }
    DeliveryOrder FindOrderForPickup(Building restaurant)
    {
        foreach (DeliveryOrder order in currentOrders)
        {
            if (order.restaurantBuilding == restaurant && order.state == OrderState.WaitingPickup)
            {
                return order;
            }
        }
        return null;
    }

    DeliveryOrder FindOrderForDelivery(Building customer)
    {
        foreach (DeliveryOrder order in currentOrders)
        {
            if (order.customerBuilding == customer && order.state == OrderState.PickedUP)
            {
                return order;
            }
        }
        return null;
    }


    public void OnDriverEnteredRstaurant(Building restaurant)
    {
        DeliveryOrder orderToPickuo = FindOrderForPickup(restaurant);

        if (orderToPickuo != null)
        {
            PickupOrder(orderToPickuo);
        }
    }
    public void OnDriverEnteredCustomer(Building customer)
    {
        DeliveryOrder orderToDeliver = FindOrderForDelivery(customer);

        if (orderToDeliver != null)
        {
            CompleteOrder(orderToDeliver);
        }
    }

    IEnumerator GeneratelnitialOrders()
    {
        yield return new WaitForSeconds(1f);

        //�����Ҷ� 3�� �ֹ� ����
        for (int i = 0; i < 3; i++)
        {
            CreataNewOrder();
            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator orderGenerator()
    {
        while (true)
        {
            yield return new WaitForSeconds(ordergeneratelnterval);

            if (currentOrders.Count < maxActiveOrders)
            {
                CreataNewOrder();
            }
        }
    }
    IEnumerator ExpiredOrderChecker()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            List<DeliveryOrder> expiredOrders = new List<DeliveryOrder>();

            foreach (DeliveryOrder order in currentOrders)
            {
                if (order.IsExpired() && order.state != OrderState.Completed)
                {
                    expiredOrders.Add(order);
                }
            }
            foreach (DeliveryOrder expired in expiredOrders)
            {
                ExpireOrder(expired);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 1300));

        GUILayout.Label("===��� �ֹ�===");
        GUILayout.Label($"���� �ֹ� ��: {currentOrders.Count}��");
        GUILayout.Label($"�Ⱦ� ��� ��: {GetPickWaitingCount()}��");
        GUILayout.Label($"��� ��� ��: {GetDeliveryWaitingCount()}��");
        GUILayout.Label($"�Ϸ� : {completedOrders}�� | ����: {expiredOrders}");

        GUILayout.Space(10);

        foreach (DeliveryOrder order in currentOrders)
        {
            string status = order.state == OrderState.WaitingPickup ? "�Ⱦ� ��� ��" : "��� ��� ��";
            float timeLeft = order.GetRemainingTime();

            GUILayout.Label($"#{order.orderId}:{order.restaurantName} -> {order.customerName}");
            GUILayout.Label($"{status} | {timeLeft:F0}�� ����");
        }
        GUILayout.EndArea();
    }
}

