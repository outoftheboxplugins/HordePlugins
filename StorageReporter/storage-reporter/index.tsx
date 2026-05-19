import { registerHordePlugin, MountType } from "../../../plugins";
import { StorageReportView } from "./storageReportView";

registerHordePlugin({
   id: "storagereporter",
   routes: [{ path: "storage-reports", element: <StorageReportView /> }],
   mount: { 
      type: MountType.TopNav, 
      context: "Tools", 
      text: "Storage Reports", 
      route: "/storage-reports"
   },
});
