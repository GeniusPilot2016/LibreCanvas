# Magic Selection fix

The Magic Selection tool now remains a mask-only selection while it is active.

Key changes in `Carpathia/ImageEditor.cs`:
- Magic Wand no longer creates or renders `SelectedBitmap`.
- Magic Wand no longer creates a `SelectionRectangle` or transform/resize box.
- Selection data is a `BitArray` (1 bit per pixel).
- The contiguous region is built with a scanline flood fill to reduce queue/RAM usage.
- The visible selection is precomputed as merged boundary segments and rendered as black/white marching-ants dashes around the actual mask boundary.
- Copy materializes only the mask bounds with transparency outside the selected pixels.
- Cut clears only the masked pixels.

The .NET SDK is not installed in the execution environment, so the project could not be compiled here. The changes were applied directly to the uploaded source tree.
