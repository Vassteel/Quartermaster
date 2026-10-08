using Quartermaster;
int checks=0;void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
World.Reset();var chest=World.Chest(1,1);World.Select(1);int moved=0;bool done=false;
chest.Data.Set("Quartermaster.operation.fence",42L);chest.Data.Set("Quartermaster.operation.until",999999999L);
Check(NativeStorageAccess.CanWrite(chest),"stale saved fences are ignored");
Check(NativeStorageAccess.Start("local",new[]{chest},()=>true,()=>moved++,afterRelease:()=>done=true),"local work executes immediately");
Check(moved==1&&done&&World.Messages.Count==0,"no network request or lock queue");
World.Select(2);Check(!NativeStorageAccess.Start("remote",new[]{chest},()=>true,()=>moved++),"remote ownership is never stolen");
Check(moved==1&&chest.Data.Owner==1,"remote refusal cannot spend inventory");
World.Select(1);chest.GetComponent<Container>().Allowed=false;
Check(!NativeStorageAccess.Start("denied",new[]{chest},()=>true,()=>moved++),"native access enforced");
chest.GetComponent<Container>().Allowed=true;
Check(!NativeStorageAccess.Start("invalid",new[]{chest},()=>false,()=>moved++),"changed materials reject before work");
done=false;Check(!NativeStorageAccess.Start("error",new[]{chest},()=>true,()=>throw new Exception("test"),afterRelease:()=>done=true)&&!done,"failed payment never continues");
Check(NativeStorageAccess.Start("retry",new[]{chest},()=>true,()=>moved++),"failure cannot leave a lock behind");
Console.WriteLine($"PASS: {checks} native ownership and no-lock checks.");
