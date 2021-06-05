# Test that the new executable runs with the existing config.
# Redirect any output just to make sure no json parsing errors or similar can leak secrets.
# Then just replace the executable atomically using mv and restart the service.
./core_update testconfig >/dev/null 2>&1 && \
\cp core core_deploybackup && \
mv core_update core && \
systemctl --user restart tpp-dualcore
