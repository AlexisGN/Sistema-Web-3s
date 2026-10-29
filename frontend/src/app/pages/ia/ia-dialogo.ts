import {AfterViewInit,Component,ElementRef,OnDestroy,ViewEncapsulation,inject} from '@angular/core';
@Component({selector:'dialog[s3sIaDialogo]',standalone:true,template:'<ng-content />',styleUrl:'./ia-dialogo.scss',encapsulation:ViewEncapsulation.None})
export class IaDialogoComponent implements AfterViewInit,OnDestroy{
 private readonly element=inject(ElementRef<HTMLDialogElement>);private anterior:HTMLElement|null=null;
 ngAfterViewInit(){this.anterior=document.activeElement instanceof HTMLElement?document.activeElement:null;const d=this.element.nativeElement;if(typeof d.showModal==='function')d.showModal();else d.setAttribute('open','');}
 ngOnDestroy(){this.anterior?.focus();}
}
