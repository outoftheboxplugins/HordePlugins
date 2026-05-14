import { DetailsList, DetailsListLayoutMode, IColumn, SelectionMode, Spinner, Stack, Text } from "@fluentui/react";
import { useState } from "react";
import backend from "../../../src/backend";
import dashboard from "../../../src/backend/Dashboard";
import { useWindowSize } from "../../../src/base/utilities/hooks";
import { formatBytes } from "../../../src/base/utilities/stringUtills";
import { Breadcrumbs } from "../../../src/components/Breadcrumbs";
import { TopNav } from "../../../src/components/TopNav";
import { getHordeStyling } from "../../../src/styles/Styles";

type Slice = { label: string; sizeBytes: number; count: number };

const columns: IColumn[] = [
   { key: "label", name: "Namespace", fieldName: "label", minWidth: 240, maxWidth: 480 },
   { key: "size",  name: "Size",      fieldName: "sizeBytes", minWidth: 120, maxWidth: 160, onRender: (i: Slice) => <Text>{formatBytes(i.sizeBytes)}</Text> },
   { key: "count", name: "Blobs",     fieldName: "count", minWidth: 100, maxWidth: 140, onRender: (i: Slice) => <Text>{i.count.toLocaleString()}</Text> },
];

export const StorageReportView: React.FC = () => {
   const [slices, setSlices] = useState<Slice[]>();
   const windowSize = useWindowSize();

   backend.fetch.get("api/v1/storage-reporter/by-namespace").then(response => {
      setSlices((response.data as { slices: Slice[] }).slices);
   }).catch(reason => { console.error(reason); });

   const { hordeClasses, modeColors } = getHordeStyling();
   const vw = Math.max(document.documentElement.clientWidth, window.innerWidth || 0);
   const centerAlign = vw / 2 - 720;
   const key = `windowsize_storage_report_${windowSize.width}_${windowSize.height}`;

   return (
      <Stack className={hordeClasses.horde}>
         <TopNav />
         <Breadcrumbs items={[{ text: "Storage Reports" }]} />
         <Stack styles={{ root: { width: "100%", backgroundColor: modeColors.background } }}>
            <Stack style={{ width: "100%", backgroundColor: modeColors.background }}>
               <Stack style={{ position: "relative", width: "100%", height: "calc(100vh - 148px)" }}>
                  <div style={{ overflowX: "auto", overflowY: "visible" }}>
                     <Stack horizontal style={{ paddingTop: 12, paddingBottom: 48 }}>
                        <Stack key={key} style={{ paddingLeft: centerAlign }} />
                        <Stack style={{ width: 1440 }}>
                           <Stack className={hordeClasses.raised}>
                              {!slices ? <Spinner /> : (
                                 <DetailsList items={slices} columns={columns}
                                    selectionMode={SelectionMode.none}
                                    layoutMode={DetailsListLayoutMode.justified}
                                    compact />
                              )}
                           </Stack>
                        </Stack>
                     </Stack>
                  </div>
               </Stack>
            </Stack>
         </Stack>
      </Stack>
   );
};
