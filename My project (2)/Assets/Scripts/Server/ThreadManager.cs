using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    //���ν����忡�� ������ �۾����� ��� ����Ʈ 
    private static readonly List<Action> executeOnMainThread = new List<Action>();
    //���ν����忡�� ������ �۾����� ������ ��� ����Ʈ 
    private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
    //���ν����忡�� ������ �۾��� �ִ��� ���θ� ��Ÿ���� �÷��� 
    private static bool actionToExecuteOnMainThread = false;

    private void Update()
    {
        UpdateMain(); //���ν����忡�� ���� ������� �۾� ó�� 

    }
    
    /// <summary>
   /// ���ν����忡�� ������ �۾��� �߰�.
   /// �� �Լ��� �ٸ� �����忡�� ȣ��ɼ��� �ִ�
   /// </summary>
   /// <param name="_action"> ���� �����忡�� ������ �۾�(Action) </param>
       
    public static void ExecuteOnMainThread(Action _action)
    {
        if (_action == null) //�۾� null�ϰ�� log��� 
        {
            Debug.Log("No action to execute on main thread!");
            return;
        }
        //�۾� ����Ʈ�� ���� �� �� lock�� ����� ����ȭ 
        //���� �����尡 ���ÿ� �۾��� �߰��Ұ�� �浹 ���� 
        lock (executeOnMainThread)
        {
            executeOnMainThread.Add(_action); //�۾��� ���ν����� 
            actionToExecuteOnMainThread = true; //�۾���  �ִٴ� �÷��� ���� 
        }
    }

    /// <summary>
    /// ���ν����忡�� ���� ������� ��� �۾��� ����
    /// ���� : �ݵ�� ���ν����忡�� ȣ����  �ؾ��� 
    /// </summary>
    public static void UpdateMain()
    {
        if (actionToExecuteOnMainThread) //������ �۾��� ������� 
        { 
            executeCopiedOnMainThread.Clear(); //���纻 ����Ʈ �ʱ�ȭ 

            //���� ������ �۾� ����Ʈ�� ���纻 ����Ʈ�� ����
            //���� �� ���� ����Ʈ�� �ʱ�ȭ�� ����ȭ ���� ����  ��
            lock (executeOnMainThread)
            {
                executeCopiedOnMainThread.AddRange(executeOnMainThread); //����
                executeOnMainThread.Clear(); //���� �ʱ�ȭ 
                actionToExecuteOnMainThread = false; //���� �� �÷��� ���� 
            }
            //���纻 �� �ִ� ��� �۾��� ���� 
            for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
            {
                executeCopiedOnMainThread[i](); //�۾�  ���� 
            }
        }
    }
}
