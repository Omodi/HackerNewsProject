import { Routes } from '@angular/router';
import { StoryList } from './components/story-list/story-list';

export const routes: Routes = [
  { path: '', component: StoryList },
  { path: '**', redirectTo: '' }
];
