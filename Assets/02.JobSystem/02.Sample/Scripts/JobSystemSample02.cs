using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace Es.JobSystem.Sample._02
{
    public class JobSystemSample02 : MonoBehaviour
    {
        // Jobの並列化のためにIJobParallelForを実装するよう変更
        struct VelocityJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Vector3> velocity;

            public NativeArray<Vector3> position;

            public float deltaTime;

            // 並列アクセスのためにインデックスを受け取って処理を行うExecuteを実装
            public void Execute(int i)
            {
                position[i] = position[i] + velocity[i] * deltaTime;
            }
        }

        public void Update()
        {
            var position = new NativeArray<Vector3>(100000, Allocator.Persistent);

            var velocity = new NativeArray<Vector3>(100000, Allocator.Persistent);
            for (var i = 0; i < velocity.Length; i++)
                velocity[i] = new Vector3(0, 10, 0);

            var job = new VelocityJob()
            {
                deltaTime = Time.deltaTime,
                position = position,
                velocity = velocity
            };

            // 並列実行のJobをスケジュールします。
            // 最初のパラメータは、各反復が何回実行されるかです。
            // 2番目のパラメータは、内部でのループ分割数(バッチ数)です。
            JobHandle jobHandle = job.Schedule(position.Length, 128);

            // メインスレッドで何か計算している最中にJobを動かしておきたい場合は以下のメソッドを呼ぶ
            JobHandle.ScheduleBatchedJobs();

            // ......
            // 何かMainThreadで行っておきたい処理
            // MainThreadで10[ms]かかる重い処理を想定
            // ......
            System.Threading.Thread.Sleep(10);

            jobHandle.Complete();

            Debug.Log(job.position[0]);

            position.Dispose();
            velocity.Dispose();
        }
    }
}