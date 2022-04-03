import { Dialog, DialogActions, DialogContent, DialogContentText } from "@mui/material";
import Button from "@mui/material/Button";
import { useState } from "react";
import AccountDetails from "../../DataClasses/AccountDetails";

interface ClientSettingsProps {
    accountDetails: AccountDetails;
    editProfile: () => void;
    deleteProfile: () => void;
}

const ClientSettings = (props: ClientSettingsProps) => {

    const [open,setOpen] = useState(false);
    const openDeleteDialog = () => {
        setOpen(true);
    }

    const cancelDeleteDialog = () => {
        setOpen(false);
    }

    const confirmDeleteDialog = () => {
        props.deleteProfile();
        setOpen(false);
    }

    return (
        <>
            <p><strong>Imię: </strong>{props.accountDetails.given_name}</p>
            <p><strong>Nazwisko: </strong>{props.accountDetails.family_name}</p>
            <p><strong>Adres: </strong>{props.accountDetails.streetAddress}, {props.accountDetails.postalCode} {props.accountDetails.city}</p>
            <p><strong>Kraj: </strong>{props.accountDetails.country}</p>
            <p><strong>E-mail: </strong>{props.accountDetails.emails[0]}</p>
            <Button onClick={props.editProfile}>Edytuj profil</Button>
            <Button onClick={openDeleteDialog} style={{color: 'red'}}>Usuń konto</Button>

            <Dialog open={open} onClose={cancelDeleteDialog} aria-describedby='alert-dialog-description'>
                <DialogContent>
                    <DialogContentText id='alert-dialog-description'>
                        Czy na pewno chcesz usunąć konto?
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={confirmDeleteDialog} style={{color:'red'}}>Tak</Button>
                    <Button onClick={cancelDeleteDialog} autoFocus>Nie</Button>
                </DialogActions>
            </Dialog>
        </>
    );
}

export default ClientSettings;