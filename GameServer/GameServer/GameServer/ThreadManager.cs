using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
     class ThreadManager
    {
        //메인스레드에서 실행할 작업들을 담는 리스트 
        private static readonly List<Action> executeOnMainThread = new List<Action>();
        //메인스레드에서 실행할 작업들을 복사해 담는 리스트 
        private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
        //메인스레드에서 실행할 작업이 있는지 여부를 나타내는 플래그 
        private static bool actionToExecuteOnMainThread = false;


        /// <summary>
        /// 메인스레드에서 실행할 작업을 추가.
        /// 이 함수는 다른 스레드에서 호출될수가 있다
        /// </summary>
        /// <param name="_action"> 메인 스레드에서 실행할 작업(Action) </param>

        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null) //작업 null일경우 log출력 
            {
                Console.WriteLine("No action to execute on main thread!");
                return;
            }
            //작업 리스트에 접근 할 때 lock을 사용해 동기화 
            //여러 스레드가 동시에 작업을 추가할경우 충돌 방지 
            lock (executeOnMainThread)
            {
                executeOnMainThread.Add(_action); //작업은 메인스레드 
                actionToExecuteOnMainThread = true; //작업이  있다는 플래그 설정 
            }
        }

        /// <summary>
        /// 메인스레드에서 실행 대기중인 모든 작업을 실행
        /// 주의 : 반드시 메인스레드에서 호출을  해야함 
        /// </summary>
        public static void UpdateMain()
        {
            if (actionToExecuteOnMainThread) //실행할 작업이 있을경우 
            {
                executeCopiedOnMainThread.Clear(); //복사본 리스트 초기화 

                //메인 스레드 작업 리스트를 복사본 리스트로 복사
                //복사 후 원본 리스트를 초기화해 동기화 문제 방지  ㄷ
                lock (executeOnMainThread)
                {
                    executeCopiedOnMainThread.AddRange(executeOnMainThread); //복사
                    executeOnMainThread.Clear(); //원본 초기화 
                    actionToExecuteOnMainThread = false; //실행 완 플래그 설정 
                }
                //복사본 에 있는 모든 작업을 실행 
                for (int i = 0; i < executeCopiedOnMainThread.Count; i++)
                {
                    executeCopiedOnMainThread[i](); //작업  실행 
                }
            }
        }
    }
}
