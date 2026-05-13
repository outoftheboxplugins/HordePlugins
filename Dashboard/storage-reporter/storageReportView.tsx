import { DetailsList, DetailsListLayoutMode, IColumn, SelectionMode, Spinner, Stack, Text } from "@fluentui/react";
import backend from "horde/backend";
import { formatBytes } from "horde/base/utilities/stringUtills";
import { Breadcrumbs } from "horde/components/Breadcrumbs";
import { TopNav } from "horde/components/TopNav";
import { getHordeStyling } from "horde/styles/Styles";
import { useState } from "react";

type Slice = { label: string; sizeBytes: number; count: number };

const columns: IColumn[] = [
   { key: "label", name: "Namespace", fieldName: "label", minWidth: 240, maxWidth: 480 },
   { key: "size",  name: "Size",      fieldName: "sizeBytes", minWidth: 120, maxWidth: 160, onRender: (i: Slice) => <Text>{formatBytes(i.sizeBytes)}</Text> },
   { key: "count", name: "Blobs",     fieldName: "count", minWidth: 100, maxWidth: 140, onRender: (i: Slice) => <Text>{i.count.toLocaleString()}</Text> },
];

export const StorageReportView: React.FC = () => {
   const [slices, setSlices] = useState<Slice[]>();

   backend.fetch.get("api/v1/storage-reporter/by-namespace").then(response => {
      setSlices((response.data as { slices: Slice[] }).slices);
   }).catch(reason => { console.error(reason); });

   const hordeClasses = getHordeStyling();
   
   return (
      <Stack className={hordeClasses.horde}>
         <TopNav />
         <Breadcrumbs items={[{ text: "Storage Reports" }]} />
         <Stack style={{ padding: 24 }}>
            {!slices ? <Spinner /> : (
               <DetailsList items={slices} columns={columns}
                  selectionMode={SelectionMode.none}
                  layoutMode={DetailsListLayoutMode.justified}
                  compact />
            )}
         </Stack>
      </Stack>
   );
};
