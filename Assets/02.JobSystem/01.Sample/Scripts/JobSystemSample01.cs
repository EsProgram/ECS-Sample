using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace Es.JobSystem.Sample._01
{
    public class JobSystemSample01 : MonoBehaviour
    {
        // Jobを作る際、Jobでアクセスされる全てのデータをJob内に宣言します。
        struct VelocityJob : IJob
        {
            // 読み取り専用として宣言することにより複数のJobが並列にデータにアクセスできます。
            [ReadOnly]
            public NativeArray<Vector3> velocity;

            // デフォルトでは、コンテナは読み書きが可能です。
            public NativeArray<Vector3> position;

            // Jobには一般的にフレームの概念がないため、deltaTimeをJobにコピーする必要があります。
            // MainThreadは同じフレームまたは次のフレームでJobを待機しますが、Jobは
            // WorkerThreadで独立して処理が実行されます。
            public float deltaTime;

            // Jobが実行するコードです。
            public void Execute ()
            {
                for (var i = 0; i < position.Length; i++)
                    position[i] = position[i] + velocity[i] * deltaTime;
            }
        }

        public void Update ()
        {
            // NativeArrayはNativeContainer属性が付加されているので
            // MainThreadとWorkerThreadで共有データとして扱われます(コピーされない)。
            // また、使い終えたらDisposeする必要があります。
            var position = new NativeArray<Vector3> (500, Allocator.Persistent);
            var velocity = new NativeArray<Vector3> (500, Allocator.Persistent);
            for (var i = 0; i < velocity.Length; i++)
                velocity[i] = new Vector3 (0, 10, 0);

            // Jobの初期化処理です。
            var job = new VelocityJob ()
            {
                deltaTime = Time.deltaTime,
                position = position,
                velocity = velocity
            };

            // Jobをスケジューリングし、後でJobの完了を待つことができるJobHandleを返します。
            JobHandle jobHandle = job.Schedule ();

            // Jobが完了したことを確認します(完了してなければ完了まで待ちます)
            // Schedule実行後、すぐにCompleteを呼び出すことはお勧めできません。
            // 並列処理の恩恵を受けることがほぼできなくなるためです。
            // フレームの早い段階でJobをScheduleし、他の処理を行った後でCompleteを呼び出すのが最適です
            jobHandle.Complete ();

            Debug.Log (job.position[0]);

            position.Dispose ();
            velocity.Dispose ();
        }
    }
}