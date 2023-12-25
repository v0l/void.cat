import "./Admin.css";
import { useState } from "react";
import { useSelector } from "react-redux";
import { Navigate } from "react-router-dom";
import { AdminProfile } from "@void-cat/api";

import { UserList } from "./UserList";
import { VoidButton } from "@/Components/Shared/VoidButton";
import VoidModal from "@/Components/Shared/VoidModal";
import EditUser from "./EditUser";

import useApi from "@/Hooks/UseApi";
import { RootState } from "@/Store";
import ImageGrid from "@/Components/Shared/ImageGrid";
import { IconButton } from "@/Components/Shared/Icon";

export function Admin() {
  const auth = useSelector((state: RootState) => state.login.jwt);
  const AdminApi = useApi();
  const [editUser, setEditUser] = useState<AdminProfile>();

  async function deleteFile(id: string) {
    if (window.confirm(`Are you sure you want to delete: ${id}?`)) {
      try {
        await AdminApi.adminDeleteFile(id);
      } catch (e) {
        console.error(e);
        alert("Failed to delete file!");
      }
    }
  }

  if (!auth) {
    return <Navigate to="/login" />;
  } else {
    return (
      <div className="admin">
        <h2>Users</h2>
        <UserList
          actions={(i) => [
            <VoidButton key={`delete-${i.id}`}>Delete</VoidButton>,
            <VoidButton key={`edit-${i.id}`} onClick={() => setEditUser(i)}>
              Edit
            </VoidButton>,
          ]}
        />

        <h2>Files</h2>
        <ImageGrid
          loadPage={(r) => AdminApi.adminListFiles(r)}
          actions={(f) => <>
            <IconButton name="trash" onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              deleteFile(f.id)
            }} />
          </>} />

        {editUser && (
          <VoidModal title="Edit user">
            <EditUser user={editUser} onClose={() => setEditUser(undefined)} />
          </VoidModal>
        )}
      </div>
    );
  }
}
